using System;
using System.Reflection;
using NUnit.Core;
using NUnit.Framework;

namespace NUnit.Engine.Drivers.Tests
{
    public class TestEventAdapterTests : ITestEventListener
    {
        private EventListener _eventAdapter;
        private string _report;

        private static TestName _testName = new TestName()
        {
            RunnerID = RUNNER_ID,
            TestID = new TestID(TEST_ID),
            Name = TEST_NAME,
            FullName = FULL_NAME
        };

        private const int RUNNER_ID = 42;
        private const int TEST_ID = 9999;
        private const string TEST_NAME = "TestName";
        private const string FULL_NAME = "FullName";

        [SetUp]
        public void Initialize()
        {
            _eventAdapter = new TestEventAdapter(this);
            _report = null;
        }

        [Test]
        public void RunStartedIsIgnored()
        {
            _eventAdapter.RunStarted("Dummy", 99);
            Assert.Null(_report, "No report should be received");
        }

        [Test]
        public void RunFinishedIsIgnored()
        {
            _eventAdapter.RunFinished(new TestResult(new TestName()));
            Assert.Null(_report, "No report should be received");
        }

        [Test]
        public void RunFinishedExceptionIsIgnored()
        {
            _eventAdapter.RunFinished(new Exception("Error!"));
            Assert.Null(_report, "No report should be received");
        }

        [Test]
        public void SuiteStarted()
        {
            var suite = new FakeTestSuite(RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME);
            _eventAdapter.SuiteStarted(suite.TestName);
            string expected = string.Format("<start-suite id=\"{0}-{1}\" name=\"{2}\" fullname=\"{3}\"/>", RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME);
            Assert.That(_report, Is.EqualTo(expected));
        }

        [Test]
        public void TestStarted()
        {
            var test = new FakeTestCase(RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME);
            _eventAdapter.TestStarted(test.TestName);
            string expected = string.Format("<start-test id=\"{0}-{1}\" name=\"{2}\" fullname=\"{3}\"/>", RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME);
            Assert.That(_report, Is.EqualTo(expected));
        }

        [Test]
        public void SuiteFinished()
        {
            var suite = new FakeTestSuite(RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME, 1234);
            var result = new TestResult(suite);
            result.SetResult(ResultState.Failure, "Something failed!", null);
            result.AssertCount = 9999;
            result.Time = 12.5;
            _eventAdapter.SuiteFinished(result);
            string expected = string.Format(
                "<test-suite type=\"TestSuite\" id=\"{0}-{1}\" name=\"{2}\" fullname=\"{3}\" runstate=\"Runnable\" testcasecount=\"1234\" result=\"Failed\" duration=\"12.500000\" total=\"1234\" passed=\"0\" failed=\"0\" inconclusive=\"0\" skipped=\"0\" asserts=\"9999\"><failure><message><![CDATA[Something failed!]]></message></failure></test-suite>", 
                RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME);
            Assert.That(_report, Is.EqualTo(expected));
        }

        [Test]
        public void TestFinished()
        {
            var test = new FakeTestCase(RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME);
            var result = new TestResult(test);
            result.SetResult(ResultState.Failure, "MESSAGE", "STACKTRACE");
            result.Time = 1.234;
            result.AssertCount = 5;
            _eventAdapter.TestFinished(result);
            string expected = 
                string.Format("<test-case id=\"{0}-{1}\" name=\"{2}\" fullname=\"{3}\" methodname=\"FakeTestMethod\" classname=\"{4}\" runstate=\"Runnable\" result=\"Failed\" duration=\"1.234000\" asserts=\"5\"><failure><message><![CDATA[MESSAGE]]></message><stack-trace><![CDATA[STACKTRACE]]></stack-trace></failure></test-case>", 
                RUNNER_ID, TEST_ID, TEST_NAME, FULL_NAME, typeof(FakeTestCase).FullName);
            Assert.That(_report, Is.EqualTo(expected));
        }

        #region ITestEventListener Implementation

        public void OnTestEvent(string report)
        {
            _report = report;
        }

        #endregion

        #region Nested Fake Test Classes

        private class FakeTestCase : TestMethod
        {
            public FakeTestCase(int runnerID, int testID, string name, string fullname)
                : base(typeof(FakeTestCase).GetMethod("FakeTestMethod", BindingFlags.NonPublic | BindingFlags.Instance))
            {
                TestName.RunnerID = runnerID;
                TestName.TestID = new TestID(testID);
                TestName.Name = name;
                TestName.FullName = fullname;
            }

            private void FakeTestMethod() { }
        }

        private class FakeTestSuite : TestSuite
        {
            private int _testCaseCount;

            public FakeTestSuite(int runnerID, int testID, string name, string fullname)
                : this(runnerID, testID, name, fullname, 0) { }

            public FakeTestSuite(int runnerID, int testID, string name, string fullname, int testCaseCount)
                : base(name)
            {
                TestName.RunnerID = runnerID;
                TestName.TestID = new TestID(testID);
                TestName.Name = name;
                TestName.FullName = fullname;

                _testCaseCount = testCaseCount;
            }

            public override int TestCount
            {
                get { return _testCaseCount; }
            }
        }

        #endregion
    }
}
