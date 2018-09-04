using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// An external thread with an own message loop.
    /// For example, a GUI thread.
    /// </summary>
    internal class ExternalThread : IThread
    {
        #region Own items
        private readonly string _name;
        #endregion

        #region Injected dependencies
        private readonly IRuntime _runtime;
        private readonly IActorLogger _logger;
        private readonly IErrorHandler _errorHandler;
        private readonly Action<Action> _dispatcher;
        #endregion

        private readonly object _lock = new Object();
        #region Guarded by _lock
        /// <summary>
        /// The actors that belong to this thread.
        /// </summary>
        private readonly Dictionary<int, IActor> _actors = new Dictionary<int, IActor>();
        #endregion

        public ExternalThread(string name, IRuntime runtime, IActorLogger logger, IErrorHandler errorHandler, Action<Action> dispatchMethod)
        {
            _name = name;
            _runtime = runtime;
            _logger = logger;
            _errorHandler = errorHandler;
            _dispatcher = dispatchMethod;
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
            IActor actor;
            lock (_lock)
            {
                if (!_actors.TryGetValue(actorId, out actor))
                {
                    return;
                }
            }

            Action toDo = () =>
                {
                    RunMessageHandler(actorId, actor, message);
                };

            _dispatcher(toDo);
        }

        private void RunMessageHandler(int actorId, IActor actor, Message message)
        {
            try
            {
                actor.OnMessage(message.Code, _runtime, actorId, message);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception in actor OnMessage", ex);
            }
        }

        public void AddActor(int actorId, IActor actor)
        {
            lock (_lock)
            {
                _actors.Add(actorId, actor);
            }
        }

        public void RemoveActor(int actorId)
        {
            IActor actor;
            lock (_lock)
            {
                if (!_actors.TryGetValue(actorId, out actor))
                {
                    return;
                }

                _actors.Remove(actorId);
            }

            Action toDo = () =>
            {
                CleanUpActor(actor);
            };

            _dispatcher(toDo);
        }

        private void CleanUpActor(IActor actor)
        {
            try
            {
                actor.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception during actor cleanup", ex);
            }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
