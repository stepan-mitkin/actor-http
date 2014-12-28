using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// Concurrent multi-actor environment.
    /// It is a container for self-sufficient actors that exchange asyncronous messages.
    /// Can have several threads inside. Each thread can run many actors.
    /// Benefits:
    /// 1. Incapsulates multithreading API. Protects against many low-level multithreading bugs.
    /// The rest of the application should not deal with threads.
    /// 2. Manages timers and timeouts.
    /// See IRuntime for the documentation on the public methods.
    /// Runtime guarantees that:
    /// 1. All methods of an actor (including CleanUp) are invoked in the same thread.
    /// 2. CleanUp is invoked when an actor is removed.
    /// 3. CleanUp is called only once.
    /// </summary>
    public class Runtime : IRuntime
    {
        private readonly IActorLogger _logger;

        private readonly RuntimeData _data;


        public Runtime(IActorLogger logger, IErrorHandler errorHandler)
        {
            _logger = logger;
            _data = new RuntimeData(logger, errorHandler);
        }

        public IActorLogger Log
        {
            get { return _logger; }
        }

        public void Dispose()
        {
            List<IThread> threads = _data.Clear();
            foreach (IThread thread in threads)
            {
                thread.Stop();
            }
        }

        /// <summary>
        /// Creates a System.Threading.Thread object and starts a message loop for it.
        /// </summary>
        public void CreateThread(string name)
        {
            IThread thread = _data.CreateThread(this, name, null);
            thread.Start();
        }

        /// <summary>
        /// Registers an external thread with an own message loop.
        /// </summary>
        /// <param name="name">Thread name.</param>
        /// <param name="dispatcherMethod">Posts work items to the external thread's message loop.</param>
        public void RegisterExternalThread(string name, Action<Action> dispatcherMethod)
        {
            _data.CreateThread(this, name, dispatcherMethod);
        }

        public int AddActor(IActor actor)
        {
            IThread thread = _data.GetRandomThread();
            int actorId = _data.AllocateActor(thread);
            thread.AddActor(actorId, actor);
            return actorId;
        }

        public int AddActorToThread(string threadName, IActor actor)
        {
            IThread thread = _data.FindThreadByName(threadName);
            int actorId = _data.AllocateActor(thread);
            thread.AddActor(actorId, actor);
            return actorId;
        }

        public int AddDedicatedActor(IActor actor)
        {
            return _data.AllocateDedicatedActor(this, actor);
        }


        public void RemoveActor(int actorId)
        {
            IThread thread = _data.DeallocateActor(actorId);
            if (thread != null)
            {
                thread.RemoveActor(actorId); 
            }
        }

        public void SendMessage(int recepient, int messageCode, object payload, int sender)
        {
            SendMessageCore(recepient, 0, messageCode, payload, sender);
        }

        public void SendResult(int caller, int callId, int resultCode, object payload, int sender)
        {
            CallResult.Check(resultCode);

            bool callFound = _data.UnregisterCall(caller, callId);

            if (callFound)
            {
                SendMessageCore(caller, callId, resultCode, payload, sender);
            }
        }


        private void SendMessageCore(int recepient, int callId, int messageCode, object payload, int sender)
        {
            if (recepient < 1)
            {
                throw new ArgumentException("Invalid recepient: " + recepient.ToString());
            }

            Message message = new Message(messageCode, callId, payload, sender);

            IThread thread = _data.FindThreadByActor(recepient);

            if (thread != null)
            {
                thread.PostMessage(recepient, message);
            }
        }

        private void OnTimer(int timerCallId, int actorId)
        {
            SendResult(actorId, timerCallId, CallResult.Timeout, null, 0);
            _data.RemoveTimer(timerCallId);
        }

        public int SendTimeout(int actorId, TimeSpan duration)
        {
            return SendTimeoutCore(actorId, 0, duration);
        }

        private int SendTimeoutCore(int actorId, int callId, TimeSpan duration)
        {
            if (actorId < 1)
            {
                throw new ArgumentException("Invalid actorId: " + actorId.ToString());
            }

            if (callId == 0)
            {
                callId = _data.RegisterCall(actorId);
            }

            TimerCallback callback = (obj) => OnTimer(callId, actorId);

            Timer timer = new Timer(callback, null, (long)duration.TotalMilliseconds, -1);

            _data.AddTimer(timer, callId, actorId);

            return callId;
        }

        public void CancelTimeout(int timerCallId)
        {
            _data.RemoveTimer(timerCallId);
        }

        public int StartVoidCall(Func<Task> method, int callerId)
        {
            int callId = _data.RegisterCall(callerId);
            Task mainTask = method();
            mainTask.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    _logger.Error(String.Format("Error. Caller id: {0}, call id: {1}.", callerId, callId), t.Exception);
                    SendResult(callerId, callId, CallResult.Error, t.Exception, 0);
                }
                else if (t.IsCanceled)
                {
                    _logger.Info(String.Format("Cancelled. Caller id: {0}, call id: {1}.", callerId, callId));
                    SendResult(callerId, callId, CallResult.Cancelled, null, 0);
                }
                else
                {
                    SendResult(callerId, callId, CallResult.Completed, null, 0);
                }
            }, TaskContinuationOptions.AttachedToParent);
            return callId;
        }

        public int SendCall(int recepient, int messageCode, object payload, int sender, TimeSpan timeout)
        {
            int callId = _data.RegisterCall(sender);
            SendMessageCore(recepient, callId, messageCode, payload, sender);
            SendTimeoutCore(sender, callId, timeout);
            return callId;
        }

        public int StartCall<T>(Func<Task<T>> method, int callerId)
        {
            int callId = _data.RegisterCall(callerId);
            Task<T> mainTask = method();
            mainTask.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    _logger.Error(String.Format("Error. Caller id: {0}, call id: {1}.", callerId, callId), t.Exception);
                    SendResult(callerId, callId, CallResult.Error, t.Exception, 0);
                }
                else if (t.IsCanceled)
                {
                    _logger.Info(String.Format("Cancelled. Caller id: {0}, call id: {1}.", callerId, callId));
                    SendResult(callerId, callId, CallResult.Cancelled, null, 0);
                }
                else
                {
                    object payload = t.Result;
                    SendResult(callerId, callId, CallResult.Completed, payload, 0);
                }
            }, TaskContinuationOptions.AttachedToParent);
            return callId;
        }

        public int StartWrite(Stream stream, IoBuffer buffer, int actorId)
        {
            if (buffer.Count == 0)
            {
                int callId = _data.NextCallId();
                SendMessageCore(actorId, callId, CallResult.Completed, null, 0);
                return callId;
            }
            else
            {
                Func<Task> writeMethod = () => stream.WriteAsync(buffer.Data, 0, buffer.Count);
                return StartVoidCall(writeMethod, actorId);
            }
        }

        public int StartRead(Stream stream, IoBuffer buffer, int actorId)
        {
            Func<Task<int>> readMethod = () => stream.ReadAsync(buffer.Data, 0, buffer.Data.Length);
            return StartCall(readMethod, actorId);
        }

        internal void RemoveThread(int actorId)
        {
            _data.RemoveThread(actorId);
        }
    }
}
