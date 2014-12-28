using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    internal class DedicatedThread : IThread
    {
        #region Own items
        private readonly string _name;
        private readonly int _actorId;
        private IActor _actor;
        #endregion

        #region Injected dependencies
        private readonly IRuntime _runtime;
        private readonly IActorLogger _logger;
        private readonly IErrorHandler _errorHandler;
        #endregion

        private readonly ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();

        public DedicatedThread(string name, IRuntime runtime, IActorLogger logger, IErrorHandler errorHandler, int actorId, IActor actor)
        {
            _actor = actor;
            _actorId = actorId;
            _name = name;
            _runtime = runtime;
            _logger = logger;
            _errorHandler = errorHandler;
        }

        public string Name
        {
            get { return _name; }
        }

        public bool IsNormal
        {
            get { return false; }
        }

        public void PostMessage(int actorId, Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            _messages.Enqueue(message);
        }

        public void AddActor(int actorId, IActor actor)
        {
            throw new NotSupportedException();
        }

        public void RemoveActor(int actorId)
        {
            if (actorId == _actorId)
            {
                Stop();
            }
        }

        public void Start()
        {
            Task.Factory.StartNew(ThreadProcedure, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _messages.Enqueue(null);
        }

        private void ThreadProcedure()
        {
            _logger.Info("Started dedicated thread " + _name);
            Message pulse = new Message(Codes.Pulse, 0, null, 0);
            while (true)
            {
                bool mustExist = ProcessMessages();
                if (mustExist)
                {
                    break;
                }

                RunMessageHandler(pulse);
            }

            Runtime runtime = (Runtime)_runtime;
            runtime.RemoveThread(_actorId);

            _logger.Info("Finished dedicated thread " + _name);
        }

        private bool ProcessMessages()
        {
            Message message;
            while (_messages.TryDequeue(out message))
            {
                if (message == null)
                {
                    CleanUpActor();
                    return true;
                }

                RunMessageHandler(message);
            }
            return false;
        }

        private void RunMessageHandler(Message message)
        {
            try
            {
                _actor.OnMessage(_runtime, _actorId, message);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception in actor OnMessage", ex);
                IActor newActor = _errorHandler.OnError(_runtime, _actorId, _actor, ex);
                if (newActor != null)
                {
                    _logger.Info(String.Format("Replacing actor {0}, type: {1}", _actorId, newActor.GetType().Name));
                    _actor = newActor;
                }
            }
        }

        private void CleanUpActor()
        {
            try
            {
                _actor.CleanUp(_runtime, _actorId);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception during actor cleanup", ex);
            }
        }
    }
}
