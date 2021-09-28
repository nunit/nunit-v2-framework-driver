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

        [TestCase("<filter><test>Some.Test.Name</test></filter>", "<name>")]
        [TestCase("<filter><test>Some.Test.Name,Another.Test.Name</test></filter>", "<name>")]
        [TestCase("<filter><id>123-456</id></filter>", "<id>")]
        [TestCase("<filter><id>123-456,123-789</id></filter>", "<or <id> <id>>")]
        [TestCase("<filter><cat>Urgent</cat></filter>", "<cat Urgent>")]
        [TestCase("<filter><cat>A,B,C</cat></filter>", "<cat A B C>")]
        [TestCase("<filter><cat>A</cat><cat>B></cat></filter>", "<and <cat A> <cat B>>")]
        [TestCase("<filter><and><cat>A</cat><cat>B></cat></and></filter>", "<and <cat A> <cat B>>")]
        [TestCase("<filter><or><cat>A</cat><cat>B></cat></or></filter>", "<or <cat A> <cat B>>")]
        [TestCase("<filter><not><cat>A</cat></not></filter>", "<not <cat A>>")]
        [TestCase("<filter><and><not><cat>Db</cat></not><not><cat>UnitTest</cat></not></and></filter>",
            "<and <not <cat Db>> <not <cat UnitTest>>>")]
        public void CreateNUnit2Filter(string input, string output)
        {
            var filter = NUnit2FrameworkDriver.CreateNUnit2TestFilter(input);
            Assert.That(V2FilterRepresentation(filter), Is.EqualTo(output));
        }

        [TestCase("<notafilter/>", NUnit2FrameworkDriver.NO_FILTER_ELEMENT_MESSAGE)]
        [TestCase("<filter><test re='1'>Some.Test.*</test></filter>", NUnit2FrameworkDriver.NO_REGULAR_EXPRESSIONS_MESSAGE)]
        [TestCase("<filter><name>NAME</name></filter>", NUnit2FrameworkDriver.NO_NAME_FILTER_MESSAGE)]
        [TestCase("<filter><class>CLASSNAME</class></filter>", NUnit2FrameworkDriver.NO_CLASS_FILTER_MESSAGE)]
        [TestCase("<filter><method>METHOD</method></filter>", NUnit2FrameworkDriver.NO_METHOD_FILTER_MESSAGE)]
        [TestCase("<filter><prop name='NAME'>VALUE</prop></filter>", NUnit2FrameworkDriver.NO_PROPERTY_FILTER_MESSAGE)]
        public void NUnitEngineExceptionTests(string input, string message)
        {
            var ex = Assert.Throws<NUnitEngineException>(() => NUnit2FrameworkDriver.CreateNUnit2TestFilter(input));
            Assert.That(ex.Message, Is.EqualTo(message));
        }

        private string V2FilterRepresentation(Core.ITestFilter filter)
        {
            string typeName = filter.GetType().Name;

            switch (typeName)
            {
                case "SimpleNameFilter":
                    return "<name>";

                case "IdFilter":
                    return "<id>";

                case "CategoryFilter":
                    var catFilter = filter as Core.Filters.CategoryFilter;
                    var sb = new StringBuilder();
                    foreach (string cat in catFilter.Categories)
                    {
                        if (cat.EndsWith(">")) // Compensate for framework bug
                            sb.Append(" " + cat.Substring(0, cat.Length - 1));
                        else
                            sb.Append(" " + cat);
                    }
                    return "<cat" + sb.ToString() + ">";

                case "NotFilter":
                    var notFilter = filter as Core.Filters.NotFilter;
                    return "<not " + V2FilterRepresentation(notFilter.BaseFilter) + ">";

                case "OrFilter":
                    var orFilter = filter as Core.Filters.OrFilter;
                    sb = new StringBuilder();
                    foreach (var f in orFilter.Filters)
                        sb.Append(" " + V2FilterRepresentation(f));
                    return "<or" + sb.ToString() + ">";

                case "AndFilter":
                    var andFilter = filter as Core.Filters.AndFilter;
                    sb = new StringBuilder();
                    foreach (var f in andFilter.Filters)
                        sb.Append(" " + V2FilterRepresentation(f));
                    return "<and" + sb.ToString() + ">";

                default:
                    return "<" + typeName + ">";
            }
        }
    }
}
