using NUnit.Framework;
using System;
using System.Threading;
using Actors;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FakeItEasy;
using System.IO;
using ActorHttp;


namespace ActorsTest
{
    [TestFixture]
    public class StreamPumpTest
    {
        private HttpActors.StreamPump _actor;
        private IoBuffer _oldInBuffer;
        private IoBuffer _oldOutBuffer;
        private IRuntime _runtime;

        [SetUp]
        public void SetUp()
        {
            _runtime = A.Fake<IRuntime>();
            _actor = new HttpActors.StreamPump();
            _actor.Runtime = _runtime;
            _actor.MyId = 888;
            _actor.InStream = new MemoryStream();
            _actor.OutStream = new MemoryStream();
            _actor.TotalLength = 100;

            _oldInBuffer = _actor.InBuffer;
            _oldOutBuffer = _actor.OutBuffer;
            _oldInBuffer.Data[0] = 1;
            _oldOutBuffer.Data[0] = 2;
        }

        [TearDown]
        public void TearDown()
        {
            if (_actor != null)
            {
                _actor.InStream.Dispose();
                _actor.OutStream.Dispose();
            }
        }

        [Test]
        public void JustRead_Completed_NotFinished()
        {
            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 0, 10, 0));

            A.CallTo(() => _runtime.StartWrite(_actor.OutStream, _oldInBuffer, 888)).MustHaveHappened();
            A.CallTo(() => _runtime.StartRead(_actor.InStream, _oldOutBuffer, 888)).MustHaveHappened();

            Assert.AreEqual(10, _actor.OutBuffer.Count);
            Assert.AreEqual(10, _actor.SoFar);

            Assert.AreEqual(HttpActors.StreamPump.StateNames.ReadSend, _actor.State);
        }

        [Test]
        public void JustRead_Completed_Finished()
        {
            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 0, 100, 0));

            A.CallTo(() => _runtime.StartWrite(_actor.OutStream, _oldInBuffer, 888)).MustHaveHappened();
            A.CallTo(() => _runtime.StartRead(_actor.InStream, _oldOutBuffer, 888)).MustNotHaveHappened();

            Assert.AreEqual(100, _actor.OutBuffer.Count);
            Assert.AreEqual(100, _actor.SoFar);

            Assert.AreEqual(HttpActors.StreamPump.StateNames.SendRemaining, _actor.State);
        }

        [Test]
        public void JustRead_Failure_Removed()
        {
            _actor.OnMessage(CallResult.Error, _runtime, 888, new Message(CallResult.Error, 0, null, 0));

            A.CallTo(() => _runtime.SendMessage(_actor.Manager, ServerConstants.PumpFinished, 888, 0)).MustHaveHappened();


            Assert.AreEqual(HttpActors.StreamPump.StateNames.Destroyed, _actor.State);
        }

        [Test]
        public void JustRead_Cancelled_Removed()
        {
            _actor.OnMessage(CallResult.Cancelled, _runtime, 888, new Message(CallResult.Cancelled, 0, null, 0));

            A.CallTo(() => _runtime.SendMessage(_actor.Manager, ServerConstants.PumpFinished, 888, 0)).MustHaveHappened();

            Assert.AreEqual(HttpActors.StreamPump.StateNames.Destroyed, _actor.State);
        }

        [Test]
        public void ReadSend_CompletedRead_JustSend()
        {
            _actor.Read = 777;
            _actor.Send = 999;
            _actor.SoFar = 20;
            _actor.State = HttpActors.StreamPump.StateNames.ReadSend;

            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 777, 10, 0));


            Assert.AreEqual(10, _actor.InBuffer.Count);
            Assert.AreEqual(30, _actor.SoFar);

            Assert.AreEqual(HttpActors.StreamPump.StateNames.JustSend, _actor.State);
        }

        [Test]
        public void ReadSend_CompletedWrite_JustRead()
        {
            _actor.Read = 777;
            _actor.Send = 999;
            _actor.SoFar = 20;
            _actor.State = HttpActors.StreamPump.StateNames.ReadSend;

            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 999, 10, 0));

            Assert.AreEqual(HttpActors.StreamPump.StateNames.JustRead, _actor.State);
        }

        [Test]
        public void ReadSend_Failure_Removed()
        {
            _actor.State = HttpActors.StreamPump.StateNames.ReadSend;
            _actor.OnMessage(CallResult.Error, _runtime, 888, new Message(CallResult.Error, 0, null, 0));

            A.CallTo(() => _runtime.SendMessage(_actor.Manager, ServerConstants.PumpFinished, 888, 0)).MustHaveHappened();


            Assert.AreEqual(HttpActors.StreamPump.StateNames.Destroyed, _actor.State);
        }

        [Test]
        public void ReadSend_Cancelled_Removed()
        {
            _actor.State = HttpActors.StreamPump.StateNames.ReadSend;
            _actor.OnMessage(CallResult.Cancelled, _runtime, 888, new Message(CallResult.Cancelled, 0, null, 0));

            A.CallTo(() => _runtime.SendMessage(_actor.Manager, ServerConstants.PumpFinished, 888, 0)).MustHaveHappened();


            Assert.AreEqual(HttpActors.StreamPump.StateNames.Destroyed, _actor.State);
        }

        [Test]
        public void JustSend_Completed_NotFinished()
        {
            _actor.SoFar = 20;
            _actor.InBuffer.Count = 20;
            _actor.State = HttpActors.StreamPump.StateNames.JustSend;
            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 0, 0, 0));

            A.CallTo(() => _runtime.StartWrite(_actor.OutStream, _oldInBuffer, 888)).MustHaveHappened();
            A.CallTo(() => _runtime.StartRead(_actor.InStream, _oldOutBuffer, 888)).MustHaveHappened();

            Assert.AreEqual(HttpActors.StreamPump.StateNames.ReadSend, _actor.State);
        }

        [Test]
        public void JustSend_Completed_Finished()
        {
            _actor.SoFar = 100;
            _actor.InBuffer.Count = 20;
            _actor.State = HttpActors.StreamPump.StateNames.JustSend;
            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 0, 0, 0));

            A.CallTo(() => _runtime.StartWrite(_actor.OutStream, _oldInBuffer, 888)).MustHaveHappened();
            A.CallTo(() => _runtime.StartRead(_actor.InStream, _oldOutBuffer, 888)).MustNotHaveHappened();

            Assert.AreEqual(HttpActors.StreamPump.StateNames.SendRemaining, _actor.State);
        }

        [Test]
        public void SendRemaining_Completed_Removed()
        {
            A.CallTo(() => _runtime.Log).Returns(new ConsoleLogger());
            _actor.State = HttpActors.StreamPump.StateNames.SendRemaining;
            _actor.OnMessage(CallResult.Completed, _runtime, 888, new Message(CallResult.Completed, 0, null, 0));

            A.CallTo(() => _runtime.SendMessage(_actor.Manager, ServerConstants.PumpFinished, 888, 0)).MustHaveHappened();


            Assert.AreEqual(HttpActors.StreamPump.StateNames.Destroyed, _actor.State);
        }
    }
}
