﻿// ***********************************************************************
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
        private const int TESTCASECOUNT = 76;
        private const int EXPLICIT_TESTS = 1;
        private const int EXPLICITFILTERING_CATEGORY = 2;

        private static readonly string V2_TEST_PATH = Path.Combine(TestContext.CurrentContext.TestDirectory, V2_TEST_DIR);
        private static readonly string ASSEMBLY_PATH = Path.Combine(V2_TEST_PATH, ASSEMBLY_NAME);

        private NUnit2FrameworkDriver _driver;

        [OneTimeSetUp]
        public void CreateDriver()
        {
            Console.WriteLine(ASSEMBLY_PATH);
            var domain = AppDomain.CreateDomain(V2_DOMAIN_NAME, null, V2_TEST_PATH, null, false);
            _driver = new NUnit2FrameworkDriver(domain) { ID = "1" };
            var settings = new Dictionary<string, object>();
            PerformBasicResultChecks(_driver.Load(ASSEMBLY_PATH, settings));
        }

        [TestCase("<filter><cat>ExplicitFiltering</cat></filter>", EXPLICITFILTERING_CATEGORY)]
        [TestCase("<filter><not><not><cat>ExplicitFiltering</cat></not></not></filter>", EXPLICIT_TESTS)]
        // Running this case last demonstrates that the loaded test is not changed
        // as a result of the filtering in the firt tests.
        [TestCase(EMPTY_FILTER, TESTCASECOUNT - EXPLICIT_TESTS)]
        public void Explore(string filter, int expectedCount)
        {
            XmlNode result = GetResult(_driver.Explore(filter));
            PerformBasicResultChecks(_driver.Explore(filter));
            Assert.That(result.SelectNodes("//test-case").Count, Is.EqualTo(expectedCount));
        }

        [Test]
        public void CountTestCases()
        {
            Assert.That(_driver.CountTestCases(EMPTY_FILTER), Is.EqualTo(TESTCASECOUNT -EXPLICIT_TESTS));
        }

        [Test]
        public void ExplicitTestsAreExcluded()
        {
            var filter = "<filter><not><not><cat>ExplicitFiltering</cat></not></not></filter>";
            Assert.That(_driver.CountTestCases(filter), Is.EqualTo(EXPLICIT_TESTS));
        }

        [Test]
        public void ExplicitTestsAreIncluded()
        {
            var filter = "<filter><cat>ExplicitFiltering</cat></filter>";
            Assert.That(_driver.CountTestCases(filter), Is.EqualTo(2));
        }

        [Test]
        public void Run()
        {
            XmlNode result = GetResult(_driver.Run(null, EMPTY_FILTER));
            PerformBasicResultChecks(result);
            Assert.That(result.SelectNodes("//test-case").Count, Is.EqualTo(TESTCASECOUNT-EXPLICIT_TESTS));
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
            Assert.That(GetAttribute(result, "name"), Is.EqualTo(ASSEMBLY_PATH));
            Assert.That(GetAttribute(result, "runstate"), Is.EqualTo("Runnable"));
            Assert.That(GetAttribute(result, "testcasecount"), Is.EqualTo(TESTCASECOUNT.ToString()));
        }
    }
}
