using System;
using System.Threading;
using System.Collections.Generic;

namespace runner
{
	class Program
	{
		public static void Main(string[] args)
		{
			var runner = 
				new TestRunner()
					.HandleTestStartedThrough(
						(test) => Console.WriteLine("Running test: " + test))
					.HandleTestFeedbackThrough(
						(msg) => Console.WriteLine("\t" + msg));

			runner.AddTest(new Test1());
			runner.Run();

			var isAborting = false;
			var start = DateTime.Now;
			while (runner.IsRunning) {
				if (!isAborting) {
					if (weHaveWaitedMoreThanFiveSecondsFrom(start)) {
						runner.Abort();
						isAborting = true;
						Console.WriteLine("Fuck this shit, this takes too long...");
					}
				}
				Thread.Sleep(10);
			}
		}

		private static bool weHaveWaitedMoreThanFiveSecondsFrom(DateTime startTime) {
			return DateTime.Now > startTime.AddSeconds(5);
		}

		interface ITest
		{
			string Name { get; }
			bool IsRunning { get; }
			void HandleFeedbackThrough(Action<string> feedbackHandler);
			void Run();
			void Abort();
		}

		class TestRunner
		{
			private bool _abortRun;
			private Action<string> _testFeedbackHandler = (msg) => {};
			private Action<string> _testStarted = (test) => {};
			private List<ITest> _tests = new List<ITest>();

			public bool IsRunning { get; private set; }

			public TestRunner HandleTestFeedbackThrough(Action<string> testFeedbackHandler) {
				_testFeedbackHandler = testFeedbackHandler;
				return this;
			}

			public TestRunner HandleTestStartedThrough(Action<string> testStarted) {
				_testStarted = testStarted;
				return this;
			}

			public void Run() {
				new Thread(runAllTests).Start();
			}

			public void Abort() {
				_abortRun = true;
			}

			public void AddTest(ITest test) {
				test.HandleFeedbackThrough(_testFeedbackHandler);
				_tests.Add(test);
			}

			private void runAllTests() {
				IsRunning = true;
				_abortRun = false;
				foreach (var test in _tests) {
					try {
						runTest(test);
						if (_abortRun)
							break;
					} catch (Exception ex) {
						_testFeedbackHandler("PANIC test failed:");
						_testFeedbackHandler(ex.ToString());
					}
				}
				IsRunning = false;
			}

			private void runTest(ITest test) {
				var testIsAborting = false;
				_testStarted(test.Name);
				test.Run();
				while (test.IsRunning) {
					if (_abortRun && !testIsAborting) {
						test.Abort();
						testIsAborting = true;
					}
				}
			}
		}

		class Test1 : ITest
		{
			private bool _isAborting = false;
			private Action<string> _feedbackHandler = (msg) => {};

			public string Name { get { return "Test 1"; } }
			public bool IsRunning { get; private set; }

			public void HandleFeedbackThrough(Action<string> feedbackHandler) {
				_feedbackHandler = feedbackHandler;
			}

			public void Run() {
				IsRunning = true;
				new Thread(runTest).Start();
			}

			public void Abort()	 {
				_isAborting = true;
			}	

			private void runTest() {
				_isAborting = false;	
				for (int i = 1; i < 10; i++) {
					if (_isAborting)
						break;
					_feedbackHandler("Running step " + i.ToString());
					Thread.Sleep(1000);
				}
				IsRunning = false;
			}
		}
	}
}