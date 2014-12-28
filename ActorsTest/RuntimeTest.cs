using NUnit.Framework;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Actors;

namespace ActorsTest
{
	[TestFixture ()]
	public class RuntimeTest
	{

		private class LogActor : IActor
		{
			public ConcurrentBag<string> History;
			public AutoResetEvent FinishTest;

			#region IActor implementation
			public void OnMessage (IRuntime runtime, int myActorId, Message message)
			{
				string thread = GetThread ();
				string entry = String.Format("OnMessage {0} {1} {2} {3}", thread, myActorId, message.Code, message.Sender);
				History.Add(entry);
				SetEvent(FinishTest);
			}
			public void CleanUp (IRuntime runtime, int myActorId)
			{
				string thread = GetThread ();
				string entry = String.Format("CleanUp {0} {1}", thread, myActorId);
				History.Add(entry);
                SetEvent(FinishTest);
			}
			#endregion

			private string GetThread()
			{
				return Thread.CurrentThread.Name;
			}
		}

        private static void SetEvent(AutoResetEvent evt)
        {
            try
            {
                evt.Set();
            }
            catch (ObjectDisposedException)
            {

            }
        }

		private class Resender : IActor
		{
			public int Target;
			#region IActor implementation
			public void OnMessage (IRuntime runtime, int myActorId, Message message)
			{
				runtime.SendMessage (Target, message.Code, message.Payload, myActorId);
			}

			public void CleanUp (IRuntime runtime, int myActorId)
			{
			}
			#endregion

			private string GetThread()
			{
				return Thread.CurrentThread.Name;
			}
		}

		private AutoResetEvent _finishTest;
		private Runtime _runtime;
		private ConcurrentBag<string> _history;
		private IActorLogger _logger = new ConsoleLogger ();
		private IErrorHandler _handler = new DefaultErrorHandler ();

		[SetUp]
		public void SetUp() {
			_finishTest = new AutoResetEvent (false);
			_history = new ConcurrentBag<string> ();
			_runtime = new Runtime (_logger, _handler);
			_runtime.CreateThread ("T1");
			_runtime.CreateThread ("T2");
		}

		[TearDown]
		public void TearDown() {
			if (_runtime != null) {
				_runtime.Dispose ();
			}
			_finishTest.Dispose ();
		}

		[Test ()]
		public void OnMessage_CleanUp_OnSameThread ()
		{
			LogActor actor = new LogActor { History = _history, FinishTest = _finishTest };
			int actorId = _runtime.AddActorToThread ("T1", actor);
			_runtime.SendMessage (actorId, 20, null, 4000);
			_finishTest.WaitOne ();
			_runtime.RemoveActor (actorId);
			_finishTest.WaitOne ();

			CollectionAssert.AreEquivalent (
				new string[] { "OnMessage T1 1 20 4000", "CleanUp T1 1" },
				_history
			);
		}

		[Test]
		public void SendMessage_SameThread()
		{
			LogActor logActor = new LogActor { History = _history, FinishTest = _finishTest };
			int actorId = _runtime.AddActorToThread ("T1", logActor);
			Resender resender = new Resender { Target = actorId };
			int resenderId = _runtime.AddActorToThread("T1", resender);
			_runtime.SendMessage(resenderId, 20, null, 4000);
			_finishTest.WaitOne();

			CollectionAssert.AreEquivalent (
				new string[] { "OnMessage T1 1 20 2" },
				_history
			);
		}

		[Test]
		public void SendMessage_DifferentThread()
		{
			LogActor logActor = new LogActor { History = _history, FinishTest = _finishTest };
			int actorId = _runtime.AddActorToThread ("T1", logActor);
			Resender resender = new Resender { Target = actorId };
			int resenderId = _runtime.AddActorToThread("T2", resender);

			_runtime.SendMessage(resenderId, 20, null, 4000);
			_finishTest.WaitOne();

			CollectionAssert.AreEquivalent (
				new string[] { "OnMessage T1 1 20 2" },
				_history
			);

		}

		[Test ()]
		public void Shutdown_calls_CleanUp ()
		{
			LogActor actor = new LogActor { History = _history, FinishTest = _finishTest };
			_runtime.AddActorToThread ("T2", actor);
			_runtime.Dispose ();
			_runtime = null;
			_finishTest.WaitOne ();

			CollectionAssert.AreEquivalent (
				new string[] { "CleanUp T2 1" },
				_history
			);
		}

		private const int Start = 88888;

		class Caller : IActor
		{
			public ConcurrentBag<string> History;
			public AutoResetEvent Finished;
			public int Target;
			#region IActor implementation
			public void OnMessage (IRuntime runtime, int myActorId, Message message)
			{
				switch (message.Code) {
				case CallResult.Completed:
					History.Add (String.Format ("Completed {0} {1}", myActorId, message.Payload));
					Finished.Set ();
					break;
				case CallResult.Error:
					History.Add (String.Format ("Failure {0}", myActorId));
					Finished.Set ();
					break;
				case CallResult.Cancelled:
					History.Add (String.Format ("Cancelled {0}", myActorId));
					Finished.Set ();
					break;
				case CallResult.Timeout:
					History.Add (String.Format ("Timeout {0}", myActorId));
					Finished.Set ();
					break;
				case Start:
					runtime.SendCall (Target, 666, null, myActorId, TimeSpan.FromSeconds (0.5));
					break;
				default:
					break;
				}
			}
			public void CleanUp (IRuntime runtime, int myActorId)
			{

			}
			#endregion
		}

		class Callee : IActor
		{
			public int Result;
			public void OnMessage (IRuntime runtime, int myActorId, Message message)
			{
				switch (Result) {
				case CallResult.Completed:
					runtime.SendResult (message.Sender, message.Id, CallResult.Completed, "hi", myActorId);
					break;
				case CallResult.Error:
					runtime.SendResult (message.Sender, message.Id, CallResult.Error, null, myActorId);
					break;
				case CallResult.Cancelled:
					runtime.SendResult (message.Sender, message.Id, CallResult.Cancelled, null, myActorId);
					break;
				case CallResult.Timeout:
					break;
				default:
					break;
				}
			}
			public void CleanUp (IRuntime runtime, int myActorId)
			{

			}
		}

		[Test]
		public void SendCall_SendResult_Completed()
		{
			Callee callee = new Callee { Result = CallResult.Completed };
			int target = _runtime.AddActor (callee);
			Caller caller = new Caller { History = _history, Finished = _finishTest, Target = target };
			int callerId = _runtime.AddActor (caller);
			_runtime.SendMessage (callerId, Start, null, 0);
			_finishTest.WaitOne ();

			CollectionAssert.AreEquivalent (
				new string[] { "Completed 2 hi" },
				_history
			);
		}

		[Test]
		public void SendCall_SendResult_Cancelled()
		{
			Callee callee = new Callee { Result = CallResult.Cancelled };
			int target = _runtime.AddActor (callee);
			Caller caller = new Caller { History = _history, Finished = _finishTest, Target = target };
			int callerId = _runtime.AddActor (caller);
			_runtime.SendMessage (callerId, Start, null, 0);
			_finishTest.WaitOne ();

			CollectionAssert.AreEquivalent (
				new string[] { "Cancelled 2" },
				_history
			);
		}

		[Test]
		public void SendCall_SendResult_Failure()
		{
			Callee callee = new Callee { Result = CallResult.Error };
			int target = _runtime.AddActor (callee);
			Caller caller = new Caller { History = _history, Finished = _finishTest, Target = target };
			int callerId = _runtime.AddActor (caller);
			_runtime.SendMessage (callerId, Start, null, 0);
			_finishTest.WaitOne ();

			CollectionAssert.AreEquivalent (
				new string[] { "Failure 2" },
				_history
			);
		}

		[Test]
		public void SendCall_SendResult_Timeout()
		{
			Callee callee = new Callee { Result = CallResult.Timeout };
			int target = _runtime.AddActor (callee);
			Caller caller = new Caller { History = _history, Finished = _finishTest, Target = target };
			int callerId = _runtime.AddActor (caller);
			_runtime.SendMessage (callerId, Start, null, 0);
			_finishTest.WaitOne ();

			CollectionAssert.AreEquivalent (
				new string[] { "Timeout 2" },
				_history
			);
		}

        [Test]
        public void SendTimeout()
        {
            LogActor actor = new LogActor { History = _history, FinishTest = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.SendTimeout(actorId, TimeSpan.FromSeconds(0.2));
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "OnMessage T1 1 4 0" },
                _history
            );
        }

        [Test]
        public void CancelTimeout()
        {
            LogActor actor = new LogActor { History = _history, FinishTest = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            int timeout = _runtime.SendTimeout(actorId, TimeSpan.FromSeconds(0.5));
            Thread.Sleep(100);
            _runtime.CancelTimeout(timeout);
            Thread.Sleep(1000);

            CollectionAssert.AreEquivalent(
                new string[] {},
                _history
            );
        }

        [Test]
        public void StartVoidCall_Completed()
        {
            Caller actor = new Caller { History = _history, Finished = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.StartVoidCall(async () =>
                {
                    await Task.Delay(50);
                }, actorId);
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "Completed 1 "},
                _history
            );
        }

        [Test]
        public void StartVoidCall_Failed()
        {
            Caller actor = new Caller { History = _history, Finished = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.StartVoidCall(async () =>
            {
                await Task.Delay(50);
                throw new Exception("oi");
            }, actorId);
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "Failure 1" },
                _history
            );
        }

        [Test]
        public void StartVoidCall_Cancelled()
        {
            Caller actor = new Caller { History = _history, Finished = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.StartVoidCall(async () =>
            {
                await Task.Delay(50);
                throw new TaskCanceledException("oi");
            }, actorId);
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "Cancelled 1" },
                _history
            );
        }

        [Test]
        public void StartCall_Completed()
        {
            Caller actor = new Caller { History = _history, Finished = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.StartCall(async () =>
            {
                await Task.Delay(50);
                return 77;
            }, actorId);
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "Completed 1 77" },
                _history
            );
        }

        [Test]
        public void StartCall_Failed()
        {
            Caller actor = new Caller { History = _history, Finished = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.StartCall(Fail, actorId);
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "Failure 1" },
                _history
            );
        }

        private static Task<int> Fail()
        {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();
            source.SetException(new Exception("oi"));
            return source.Task;
        }

        private static Task<int> Cancel()
        {
            TaskCompletionSource<int> source = new TaskCompletionSource<int>();
            source.SetCanceled();
            return source.Task;
        }

        [Test]
        public void StartCall_Cancelled()
        {
            Caller actor = new Caller { History = _history, Finished = _finishTest };
            int actorId = _runtime.AddActorToThread("T1", actor);
            _runtime.StartCall(Cancel, actorId);
            _finishTest.WaitOne();

            CollectionAssert.AreEquivalent(
                new string[] { "Cancelled 1" },
                _history
            );
        }
	}
}

