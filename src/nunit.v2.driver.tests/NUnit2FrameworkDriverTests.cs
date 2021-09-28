// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
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
using System.Diagnostics;
using System.IO;
using System.Xml;
using NUnit.Framework;

namespace NUnit.Engine.Drivers.Tests
{
    public class NUnit2FrameworkDriverTests
    {
        private const string ASSEMBLY_NAME = "v2-test-assembly.dll";
        private const string V2_TEST_DIR = "v2-tests";
        private const string V2_DOMAIN_NAME = "v2_test_domain";
        private const string EMPTY_FILTER = "<filter/>";
        private const int ALL_TESTS = 77;
        private const int EXPLICIT_TESTS = 1;
        private const int NON_EXPLICIT_TESTS = 76;

        private const int EXPLICIT_CATEGORY = 1;
        private const int ANOTHER_CATEGORY = 2;

        private static readonly string V2_TEST_PATH = Path.Combine(TestContext.CurrentContext.TestDirectory, V2_TEST_DIR);
        private static readonly string ASSEMBLY_PATH = Path.Combine(V2_TEST_PATH, ASSEMBLY_NAME);

        private NUnit2FrameworkDriver _driver;

        [OneTimeSetUp]
        public void CreateDriver()
        {
            var domain = AppDomain.CreateDomain(V2_DOMAIN_NAME, null, V2_TEST_PATH, null, false);
            _driver = new NUnit2FrameworkDriver(domain) { ID = "1" };
            var settings = new Dictionary<string, object>();
            PerformBasicResultChecks(_driver.Load(ASSEMBLY_PATH, settings));
        }

        static TestCaseData[] TestCounts = new TestCaseData[]
        {
            new TestCaseData(EMPTY_FILTER, NON_EXPLICIT_TESTS),
            new TestCaseData("<filter><test>NUnit.Core.Tests.ExplicitFiltering</test></filter>", 2),
            new TestCaseData("<filter><test>NUnit.Core.Tests.ExplicitFiltering.ExplicitTest</test></filter>", 1),
            new TestCaseData("<filter><cat>ExplicitCategory</cat></filter>", EXPLICIT_TESTS),
            new TestCaseData("<filter><cat>AnotherCategory</cat></filter>", ANOTHER_CATEGORY),
            new TestCaseData("<filter><not><cat>ExplicitCategory</cat></not></filter>", NON_EXPLICIT_TESTS),
            new TestCaseData("<filter><not><cat>AnotherCategory</cat></not></filter>", NON_EXPLICIT_TESTS - ANOTHER_CATEGORY),
            new TestCaseData("<filter><not><not><cat>ExplicitCategory</cat></not></not></filter>", 0),
            new TestCaseData("<filter><or><cat>ExplicitCategory</cat><cat>AnotherCategory</cat></or></filter>", EXPLICIT_CATEGORY + ANOTHER_CATEGORY),
            new TestCaseData("<filter><or><not><cat>ExplicitCategory</cat></not><not><cat>AnotherCategory</cat></not></or></filter>", NON_EXPLICIT_TESTS),
            new TestCaseData("<filter><and><cat>ExplicitCategory</cat><cat>AnotherCategory</cat></and></filter>", 0),
            new TestCaseData("<filter><and><not><cat>ExplicitCategory</cat></not><not><cat>AnotherCategory</cat></not></and></filter>", NON_EXPLICIT_TESTS - ANOTHER_CATEGORY),
            // Running this case again last demonstrates that the loaded test is
            // not changed as a result of the filtering in the first tests.
            new TestCaseData(EMPTY_FILTER, NON_EXPLICIT_TESTS)
        };

        [TestCaseSource("TestCounts")]
        public void Explore(string filter, int expectedCount)
        {
            XmlNode result = GetResult(_driver.Explore(filter));
            PerformBasicResultChecks(_driver.Explore(filter));
            Assert.That(result.SelectNodes("//test-case").Count, Is.EqualTo(expectedCount));
        }

        [TestCaseSource("TestCounts")]
        public void CountTestCases(string filter, int expectedCount)
        {
            Assert.That(_driver.CountTestCases(filter), Is.EqualTo(expectedCount));
        }

        [Test]
        public void Run()
        {
            XmlNode result = GetResult(_driver.Run(null, EMPTY_FILTER));
            PerformBasicResultChecks(result);
            Assert.That(result.SelectNodes("//test-case").Count, Is.EqualTo(NON_EXPLICIT_TESTS));
        }

        private XmlNode GetResult(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.FirstChild;
        }

        private string GetAttribute(XmlNode node, string name)
        {
            var attr = node.Attributes[name];
            return attr != null
                ? attr.Value
                : null;
        }

        private void PerformBasicResultChecks(string xml)
        {
            PerformBasicResultChecks(GetResult(xml));
        }

        private void PerformBasicResultChecks(XmlNode result)
        {
            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(GetAttribute(result, "type"), Is.EqualTo("Assembly"));
            Assert.That(GetAttribute(result, "id"), Does.StartWith("1-"));
            Assert.That(GetAttribute(result, "name"), Is.EqualTo(ASSEMBLY_NAME));
            Assert.That(GetAttribute(result, "fullname"), Is.EqualTo(ASSEMBLY_PATH));
            Assert.That(GetAttribute(result, "runstate"), Is.EqualTo("Runnable"));
            // NOTE: This checks the attribute, not how many were actually run
            Assert.That(GetAttribute(result, "testcasecount"), Is.EqualTo(ALL_TESTS.ToString()));
        }
    }
}
