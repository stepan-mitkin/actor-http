using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// A thread inside a runtime.
    /// Owns a System.Threading.Thread object.
    /// Thread safe. Public methods can be called from any thread.
    /// </summary>
    internal class RuntimeThread : IThread
    {
        #region Own items
        /// <summary>
        /// The system thread that is owned by this. Can be null.
        /// </summary>
        private readonly Thread _thread;
        private readonly string _name;
        #endregion

        #region Injected dependencies
        private readonly IRuntime _runtime;
        private readonly IActorLogger _logger;
        private readonly IErrorHandler _errorHandler;
        #endregion

        private readonly object _queueLock = new Object();
        #region Guarded by _queueLock
        /// <summary>
        /// The actors that belong to this thread.
        /// </summary>
        private readonly Dictionary<int, IActor> _actors = new Dictionary<int, IActor>();
        /// <summary>
        /// The incoming message queue.
        /// </summary>
        private List<Parcel> _queue = new List<Parcel>();
        /// <summary>
        /// Exit flag.
        /// </summary>
        private bool _mustExit = false;
        #endregion

        #region Internal thread-local
        /// <summary>
        /// The message queue for internal processing.
        /// </summary>
        private List<Parcel> _queue2 = new List<Parcel>();
        #endregion



        public RuntimeThread(string name, IRuntime runtime, IActorLogger logger, IErrorHandler errorHandler)
        {
            _name = name;
            _runtime = runtime;
            _logger = logger;
            _errorHandler = errorHandler;
            _thread = new Thread(ThreadMessageLoop);
            _thread.Name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public bool IsNormal
        {
            get { return true; }
        }

        public void Start()
        {
            _thread.Start();
        }

        public void AddActor(int actorId, IActor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException("actor");
            }

            lock (_queueLock)
            {
                if (_actors.ContainsKey(actorId))
                {
                    throw new ArgumentException("Actor with this id already exists " + actorId);
                }
                _actors.Add(actorId, actor);
            }
        }

        public void RemoveActor(int actorId)
        {
            PostToQueue(actorId, true, null);
        }

        public void PostMessage(int actorId, Message message)
        {
            PostToQueue(actorId, false, message);
        }

        public void Stop()
        {
            lock (_queueLock)
            {
                _mustExit = true;
                // Notify the internal thread there is an incoming event.
                Monitor.Pulse(_queueLock);
            }
        }

        private void PostToQueue(int actorId, bool kill, Message message)
        {
            lock (_queueLock)
            {
                IActor actor;
                if (!_actors.TryGetValue(actorId, out actor)) return;
                Parcel parcel = new Parcel { ActorId = actorId, Actor = actor, Message = message, Kill = kill };
                _queue.Add(parcel);

                // Notify the internal thread there is an incoming event.
                Monitor.Pulse(_queueLock);
            }
        }

        /// <summary>
        /// Run by the internal thread.
        /// </summary>
        private void ThreadMessageLoop()
        {
            while (true)
            {
                // Wait for events.
                lock (_queueLock)
                {
                    // This loop is necessary.
                    while (true)
                    {
                        if (_mustExit)
                        {
                            CleanUp();
                            return;
                        }
                        if (_queue.Count == 0)
                        {
                            // Wait for incoming events.
                            Monitor.Wait(_queueLock);
                        }
                        else
                        {
                            // Got one or more events!
                            break;
                        }
                    }
                    // Could just swap here, actually.
                    _queue = Interlocked.Exchange(ref _queue2, _queue);
                }

                ProcessMessages();
            }
        }

        private void ProcessMessages()
        {
            // Process messages.
            foreach (Parcel parcel in _queue2)
            {
                if (parcel.Kill)
                {
                    CleanUpActor(parcel);
                    RemoveActorCore(parcel.ActorId);
                }
                else
                {
                    RunMessageHandler(parcel);
                }
            }
            _queue2.Clear();
        }

        private void RunMessageHandler(Parcel parcel)
        {
            try
            {
                parcel.Actor.OnMessage(_runtime, parcel.ActorId, parcel.Message);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception in actor OnMessage", ex);
                IActor newActor = _errorHandler.OnError(_runtime, parcel.ActorId, parcel.Actor, ex);
                if (newActor != null)
                {
                    _logger.Info(String.Format("Replacing actor {0}, type: {1}", parcel.ActorId, newActor.GetType().Name));
                    ReplaceActor(parcel.ActorId, newActor);
                }
            }
        }

        private void CleanUpActor(Parcel parcel)
        {
            try
            {
                parcel.Actor.CleanUp(_runtime, parcel.ActorId);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception during actor cleanup", ex);
            }
        }

        private void RemoveActorCore(int actorId)
        {
            lock (_queueLock)
            {
                _actors.Remove(actorId);
            }
        }

        private void ReplaceActor(int actorId, IActor actor)
        {
            lock (_queueLock)
            {
                _actors[actorId] = actor;
            }
        }

        private void CleanUp()
        {
            foreach (KeyValuePair<int, IActor> record in _actors)
            {
                record.Value.CleanUp(_runtime, record.Key);
            }
        }
    }
}
