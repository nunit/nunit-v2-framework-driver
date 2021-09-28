// ***********************************************************************
// Copyright (c) 2014-2019 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Core;
using NUnit.Core.Filters;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers
{
    [Extension]
    public class NUnit2FrameworkDriver : IFrameworkDriver
    {
        // TODO: The id should not be hard-coded
        private const string LOAD_RESULT_FORMAT =
            "<test-suite type='Assembly' id='{0}' name='{1}' fullname='{2}' testcasecount='0' runstate='NotRunnable'>" +
                "<properties>" +
                    "<property name='_SKIPREASON' value='{3}'/>" +
                "</properties>" +
            "</test-suite>";

        AppDomain _testDomain;
        string _testAssemblyPath;

        string _name;
        string _fullname;

        TestRunner _runner;
        Core.TestPackage _package;

        /// <summary>
        /// Create a new NUnit2FrameworkDriver
        /// </summary>
        /// <param name="testDomain">The AppDomain to use for the runner</param>
        /// <remarks>
        /// The framework assembly name is needed because this driver is used for both the
        /// nunit.framework 2.x and nunitlite 1.0.
        /// </remarks>
        public NUnit2FrameworkDriver(AppDomain testDomain)
        {
            _testDomain = testDomain;

            var initializer = DomainInitializer.CreateInstance(_testDomain);
            initializer.InitializeDomain((int)InternalTrace.Level);
        }

        public string ID { get; set; }

        private TestRunner Runner {
            get
            {
                if (_runner == null)
                {
                    int runnerId;
                    if (ID == null)
                    {
                        throw new NUnitEngineException("NUnit 2 Driver requires that ID must be defined at first.");
                    }

                    if (!int.TryParse(ID, out runnerId))
                    {
                        throw new NUnitEngineException("NUnit 2 Driver requires that ID must be defined as a string representation of an integer value.");
                    }

                    _runner = RemoteTestRunner.CreateInstance(_testDomain, runnerId);
                }

                return _runner;
            }
        }

        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            if (!File.Exists(testAssemblyPath))
                throw new ArgumentException("testAssemblyPath", "Framework driver Load called with a file name that doesn't exist.");

            _testAssemblyPath = testAssemblyPath;
            _name = Escape(Path.GetFileName(_testAssemblyPath));
            _fullname = Escape(_testAssemblyPath);

            _package = new Core.TestPackage(_testAssemblyPath);
            foreach (var key in settings.Keys)
                _package.Settings[key] = settings[key];

            if (!Runner.Load(_package))
                return string.Format(LOAD_RESULT_FORMAT, TestID, _name, _fullname, "No tests were found");

            Core.ITest test = Runner.Test;
            // TODO: Handle error where test is null

            var xmlNode = test.ToXml(false);
            return test.ToXml(false).OuterXml;
        }

        public int CountTestCases(string filter)
        {
            ITestFilter v2Filter = CreateNUnit2TestFilter(filter);
            return Runner.CountTestCases(v2Filter);
        }

        public string Run(ITestEventListener listener, string filter)
        {
            if (Runner.Test == null)
                return String.Format(LOAD_RESULT_FORMAT, TestID, _name, _fullname, "Error loading test");

            ITestFilter v2Filter = CreateNUnit2TestFilter(filter);

            var result = Runner.Run(new TestEventAdapter(listener), v2Filter, false, LoggingThreshold.Off);

            return result.ToXml(true).OuterXml;
        }

        public string Explore(string filter)
        {
            if (Runner.Test == null)
                return String.Format(LOAD_RESULT_FORMAT, TestID, _name, _fullname, "Error loading test");

            ITestFilter v2Filter = CreateNUnit2TestFilter(filter);

            return Runner.Test.ToXml(v2Filter).OuterXml;
        }

        public void StopRun(bool force)
        {
            Runner.CancelRun();
        }

        private static string Escape(string original)
        {
            return original
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private string TestID
        {
            get { return string.IsNullOrEmpty(ID) ? "1" : ID + "-1";}
        }

        // Constants are public so they can be used by tests
        public const string NO_FILTER_ELEMENT_MESSAGE = "Invalid filter passed to NUnit V2 driver: no filter element at top level";
        public const string NO_REGULAR_EXPRESSIONS_MESSAGE = "Filters with regular expressions are only supported when running NUnit 3 tests";
        public const string NO_ID_FILTER_MESSAGE = "Filtering on id is only valid when running NUnit 3 tests";
        public const string NO_NAME_FILTER_MESSAGE = "Filtering on name is only valid when running NUnit 3 tests";
        public const string NO_CLASS_FILTER_MESSAGE = "Filtering on class is only valid when running NUnit 3 tests";
        public const string NO_METHOD_FILTER_MESSAGE = "Filtering on method is only valid when running NUnit 3 tests";
        public const string NO_PROPERTY_FILTER_MESSAGE = "Filtering on property value is only valid when running NUnit 3 tests";
        public const string INVALID_FILTER_MESSAGE = "Invalid filter passed to the NUnit V2 driver: {0} is not a known filter type";

        public static ITestFilter CreateNUnit2TestFilter(string filterXml)
        {
            if (string.IsNullOrEmpty(filterXml))
                return Core.TestFilter.Empty;

            var doc = new XmlDocument();
            doc.LoadXml(filterXml);
            var topNode = doc.FirstChild;
            if (topNode.Name != "filter")
                throw new NUnitEngineException(NO_FILTER_ELEMENT_MESSAGE);

            ITestFilter filter;
            switch (topNode.ChildNodes.Count)
            {
                case 0:
                    filter = Core.TestFilter.Empty;
                    break;

                case 1:
                    filter = FromXml(topNode.FirstChild);
                    break;

                default:
                    filter = FromXml(topNode);
                    break;
            }

            if (filter is Core.Filters.NotFilter)
              ((Core.Filters.NotFilter)filter).TopLevel = true;

            return filter;
        }

        private static readonly char[] COMMA = { ',' };

        private static Core.TestFilter FromXml(XmlNode xmlNode)
        {
            switch (xmlNode.Name)
            {
                case "filter":
                case "and":
                    var andFilter = new Core.Filters.AndFilter();
                    foreach (XmlNode childNode in xmlNode.ChildNodes)
                        andFilter.Add(FromXml(childNode));
                    return andFilter;

                case "or":
                    var orFilter = new Core.Filters.OrFilter();
                    foreach (System.Xml.XmlNode childNode in xmlNode.ChildNodes)
                        orFilter.Add(FromXml(childNode));
                    return orFilter;

                case "not":
                    return new Core.Filters.NotFilter(FromXml(xmlNode.FirstChild));

                case "test":
                    if (xmlNode.Attributes["re"] != null)
                        throw new NUnitEngineException(NO_REGULAR_EXPRESSIONS_MESSAGE);
                    return new Core.Filters.SimpleNameFilter(xmlNode.InnerText);

                case "cat":
                    if (xmlNode.Attributes["re"] != null)
                        throw new NUnitEngineException(NO_REGULAR_EXPRESSIONS_MESSAGE);
                    var catFilter = new Core.Filters.CategoryFilter();
                    foreach (string cat in xmlNode.InnerText.Split(COMMA))
                    {
                        if (cat.EndsWith(">"))
                        {
                            Console.WriteLine("Appending category " + cat + " from");
                            Console.WriteLine(xmlNode.OuterXml);
                        }
                        catFilter.AddCategory(cat);
                    }
                    return catFilter;

                case "id":
                    // Translate the NUnit 3 IdFilter to an NUnit V2 IdFilter. The conversion
                    // will only work with version 2.7.1 and higher of the of V2 framework.
                    OrFilter filter = new OrFilter();

                    foreach (string id in xmlNode.InnerText.Split(COMMA))
                    {
                        // All ids generated by the V2 driver are of the form xxx-yyy where xxx is the
                        // runner ID for this driver and yyy is the test ID. Both are in the form of
                        // the string representation of an integer.
                        var parts = id.Split('-');
                        int runnerID;
                        int testID;

                        // If the id is NOT of the proper form, then the test in question can't be found
                        // in this assembly, so we don't add it.
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0], out runnerID) &&
                            int.TryParse(parts[1], out testID))
                        {
                            filter.Add(new NUnit.Core.Filters.IdFilter(runnerID, new TestID(testID)));
                        }
                    }

                    return filter.Filters.Length == 1
                        ? filter.Filters[0] as NUnit.Core.TestFilter
                        : filter;

                case "name":
                    throw new NUnitEngineException(NO_NAME_FILTER_MESSAGE);
                case "class":
                    throw new NUnitEngineException(NO_CLASS_FILTER_MESSAGE);
                case "method":
                    throw new NUnitEngineException(NO_METHOD_FILTER_MESSAGE);
                case "prop":
                    throw new NUnitEngineException(NO_PROPERTY_FILTER_MESSAGE);
                default:
                    throw new NUnitEngineException(string.Format(INVALID_FILTER_MESSAGE, xmlNode.Name));
            }
        }
    }
}
