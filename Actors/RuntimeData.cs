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
    /// Thread-safe. The data structure for Runtime.
    /// All methods MUST be wrapped in a "lock" statement.
    /// Mitigates 2 kinds of risk:
    /// 1. Race conditions when different threads access the data structure.
    /// 2. A deadlock that involves the runtime lock and a thread lock.
    /// The only allowed sequence of locking is: 1. thread lock. 2. runtime lock.
    /// Therefore calling any IThread methods is not allowed here.
    /// </summary>
    internal class RuntimeData
    {

        private class TimerInfo
        {
            public Timer Timer;
            public int CallId;
            public int Actor;
        }

        /// <summary>
        /// The runtime lock.
        /// </summary>
        private readonly object _lock = new Object();

        private int _callId = 0;

        /// <summary>select * from
        /// Actor id to thread mapping.
        /// </summary>
        private readonly Dictionary<int, IThread> _actorsToThreads = new Dictionary<int, IThread>();

        /// <summary>
        /// The list of call ids for each actor that have outstanding calls.
        /// The point is to protect an actor from getting the result of a call initiated by someone else.
        /// </summary>
        private readonly Dictionary<int, List<int>> _callsInProgress = new Dictionary<int, List<int>>();

        /// <summary>
        /// Thread name to thread mapping.
        /// </summary>
        private readonly Dictionary<string, IThread> _threadsByName = new Dictionary<string, IThread>();

        /// <summary>
        /// The next actor id.
        /// </summary>
        private int _nextActorId = 1;

        /// <summary>
        /// The list of currently active timers.
        /// </summary>
        private readonly Dictionary<int, TimerInfo> _timersByCallId = new Dictionary<int, TimerInfo>();

        private readonly Random _random = new Random();


        private readonly IActorLogger _logger;
        private readonly IErrorHandler _errorHandler;

        public RuntimeData(IActorLogger logger, IErrorHandler errorHandler)
        {
            _logger = logger;
            _errorHandler = errorHandler;
        }

        public List<IThread> Clear()
        {
            lock (_lock)
            {
                List<IThread> threads = _threadsByName.Values.ToList();
                _actorsToThreads.Clear();
                _callsInProgress.Clear();
                _threadsByName.Clear();
                _timersByCallId.Clear();
                _nextActorId = 1;
                return threads;
            }
        }

        public IThread GetRandomThread()
        {
            lock (_lock)
            {
                List<IThread> threads = _threadsByName
                                        .Values
                                        .Where(t => t.IsNormal)
                                        .ToList();

                if (threads.Count == 0)
                {
                    throw new InvalidOperationException("No internal threads exist. Call CreateThread to create some.");
                }

                int index = _random.Next(0, threads.Count);
                return threads[index];
            }
        }

        public IThread CreateThread(IRuntime runtime, string name, Action<Action> dispatchMethod)
        {
            lock (_lock)
            {
                if (String.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentNullException(name);
                }
                if (_threadsByName.ContainsKey(name))
                {
                    throw new ArgumentException("Thread already exists: " + name);
                }
                IThread thread;
                if (dispatchMethod == null)
                {
                    thread = new RuntimeThread(name, runtime, _logger, _errorHandler);
                }
                else
                {
                    thread = new ExternalThread(name, runtime, _logger, _errorHandler, dispatchMethod);
                }
            
                _threadsByName.Add(name, thread);
                return thread;
            }
        }

        public int AllocateActor(IThread thread)
        {
            lock (_lock)
            {
                int actorId = _nextActorId;
                _nextActorId++;

                _actorsToThreads.Add(actorId, thread);
                return actorId;
            }
        }

        internal int AllocateDedicatedActor(IRuntime runtime, IActor actor)
        {
            lock (_lock)
            {
                int actorId = _nextActorId;
                _nextActorId++;
                string name = String.Format("Dedicated thread for actor {0}", actorId);
                IThread thread = new DedicatedThread(name, runtime, _logger, _errorHandler, actorId, actor);

                _actorsToThreads.Add(actorId, thread);
                thread.Start();
                return actorId;
            }
        }

        public IThread DeallocateActor(int actorId)
        {
            lock (_lock)
            {
                if (actorId < 0)
                {
                    throw new ArgumentException("Invalid actorId: " + actorId.ToString());
                }

                IThread thread;
                _actorsToThreads.TryGetValue(actorId, out thread);

                _actorsToThreads.Remove(actorId);
                _callsInProgress.Remove(actorId);

                return thread;
            }
        }

        public bool UnregisterCall(int actor, int call)
        {
            lock (_lock)
            {
                List<int> calls;
                if (_callsInProgress.TryGetValue(actor, out calls))
                {
                    if (calls.Contains(call))
                    {
                        calls.Remove(call);
                        return true;
                    }
                }
                return false;
            }
        }

        public int RegisterCall(int actor)
        {
            lock (_lock)
            {
                _callId++;
                int call = _callId;
                List<int> calls;
                if (!_callsInProgress.TryGetValue(actor, out calls))
                {
                    calls = new List<int>();
                    _callsInProgress.Add(actor, calls);
                }

                calls.Add(call);
                return call;
            }
        }

        public IThread FindThreadByActor(int actorId)
        {
            lock (_lock)
            {
                IThread thread;
                if (_actorsToThreads.TryGetValue(actorId, out thread))
                {
                    return thread;
                }
                return null;
            }
        }

        public IThread FindThreadByName(string threadName)
        {
            lock (_lock)
            {
                IThread thread;
                if (!_threadsByName.TryGetValue(threadName, out thread))
                {
                    throw new ArgumentException("Thread does not exists: " + threadName);
                }
                return thread;
            }
        }


        public void AddTimer(Timer timer, int callId, int actorId)
        {
            lock (_lock)
            {
                TimerInfo record = new TimerInfo { Timer = timer, CallId = callId, Actor = actorId };
                _timersByCallId[callId] = record;
            }
        }


        public void RemoveTimer(int timerCallId)
        {
            lock (_lock)
            {
                TimerInfo timer;
                if (_timersByCallId.TryGetValue(timerCallId, out timer))
                {
                    _timersByCallId.Remove(timer.CallId);
                    UnregisterCall(timer.Actor, timer.CallId);
                    timer.Timer.Dispose();
                }
            }
        }


        public int NextCallId()
        {
            lock (_lock)
            {
                _callId++;
                return _callId;
            }
        }

        public void RemoveThread(int actorId)
        {
            lock (_lock)
            {
                IThread thread;
                if (_actorsToThreads.TryGetValue(actorId, out thread))
                {
                    _actorsToThreads.Remove(actorId);
                    _threadsByName.Remove(thread.Name);
                }
            }
        }
    }
}

