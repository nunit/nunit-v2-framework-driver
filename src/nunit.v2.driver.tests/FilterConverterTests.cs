// ***********************************************************************
// Copyright (c) 2019 Charlie Poole
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
using System.Text;
using NUnit.Framework;

namespace NUnit.Engine.Drivers.Tests
{
    public class FilterConverterTests
    {
        [TestCase("<filter></filter>")]
        [TestCase("<filter/>")]
        [TestCase("")]
        [TestCase(null)]
        public void EmptyFilter(string input)
        {
            Assert.That(NUnit2FrameworkDriver.CreateNUnit2TestFilter(input).IsEmpty);
        }

        [TestCase("<filter><test>Some.Test.Name</test></filter>", typeof(Core.Filters.SimpleNameFilter))]
        [TestCase("<filter><test>Some.Test.Name,Another.Test.Name</test></filter>", typeof(Core.Filters.SimpleNameFilter))]
        [TestCase("<filter><cat>Urgent</cat></filter>", typeof(Core.Filters.CategoryFilter))]
        [TestCase("<filter><cat>A,B,C</cat></filter>", typeof(Core.Filters.CategoryFilter))]
        [TestCase("<filter><cat>A</cat><cat>B></cat></filter>", typeof(Core.Filters.AndFilter))]
        [TestCase("<filter><and><cat>A</cat><cat>B></cat></and></filter>", typeof(Core.Filters.AndFilter))]
        [TestCase("<filter><or><cat>A</cat><cat>B></cat></or></filter>", typeof(Core.Filters.OrFilter))]
        [TestCase("<filter><not><cat>A</cat></not></filter>", typeof(Core.Filters.NotFilter))]
        public void CorrectFilterType(string input, Type type)
        {
            var filter = NUnit2FrameworkDriver.CreateNUnit2TestFilter(input);
            Assert.That(filter, Is.TypeOf(type));
        }

        [TestCase("<notafilter/>", NUnit2FrameworkDriver.NO_FILTER_ELEMENT_MESSAGE)]
        [TestCase("<filter><test re='1'>Some.Test.*</test></filter>", NUnit2FrameworkDriver.NO_REGULAR_EXPRESSIONS_MESSAGE)]
        [TestCase("<filter><id>123-456</id></filter>", NUnit2FrameworkDriver.NO_ID_FILTER_MESSAGE)]
        [TestCase("<filter><name>NAME</name></filter>", NUnit2FrameworkDriver.NO_NAME_FILTER_MESSAGE)]
        [TestCase("<filter><class>CLASSNAME</class></filter>", NUnit2FrameworkDriver.NO_CLASS_FILTER_MESSAGE)]
        [TestCase("<filter><method>METHOD</method></filter>", NUnit2FrameworkDriver.NO_METHOD_FILTER_MESSAGE)]
        [TestCase("<filter><prop name='NAME'>VALUE</prop></filter>", NUnit2FrameworkDriver.NO_PROPERTY_FILTER_MESSAGE)]
        public void NUnitEngineExceptionTests(string input, string message)
        {
            var ex = Assert.Throws<NUnitEngineException>(() => NUnit2FrameworkDriver.CreateNUnit2TestFilter(input));
            Assert.That(ex.Message, Is.EqualTo(message));
        }
    }
}
