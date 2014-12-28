using Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ActorHttp
{
    public class MiniServer : IDisposable
    {
        private const int MaxPumps = 10000;
        private readonly HttpListener _listener = new HttpListener();
        private readonly int _port;
        private readonly string _folder;
        private readonly IActorLogger _logger = new ConsoleLogger();
        private readonly IErrorHandler _handler = new DefaultErrorHandler();
        private Runtime _runtime;
        private readonly CancellationTokenSource _source = new CancellationTokenSource();


        public MiniServer(int port, string folder)
        {
            _port = port;
            _folder = folder;
            _runtime = new Runtime(_logger, _handler);
            string prefix1 = String.Format("http://+:{0}/", port);
            _listener.Prefixes.Add(prefix1);
        }

        public void Start()
        {
            int logicalProcs = Environment.ProcessorCount;
            for (int i = 0; i < logicalProcs; i++)
            {
                string name = String.Format("T{0}", i + 1);
                _runtime.CreateThread(name);
            }

            Console.WriteLine("Created {0} runtime threads.", logicalProcs);

			var folderReader = new HttpActors.FolderReader();
			int readerId = _runtime.AddActor(folderReader);

			var manager = new HttpActors.PumpManager();
			manager.MaxPumps = MaxPumps;
            manager.Folder = _folder;
			manager.FolderReader = readerId;

            int managerId = _runtime.AddActor(manager);

			Console.WriteLine("Starting web server at port {0} and folder '{1}'...", _port, _folder);
			_listener.Start ();
			Console.WriteLine("Server started.");

            RunServer(_listener, managerId, _runtime, _source.Token);
        }

        public void Stop()
        {
            if (_runtime != null)
            {
                _source.Cancel();
                _runtime.Dispose();
                _listener.Close();
                _runtime = null;
            }
        }


        public void Dispose()
        {
            Stop();
        }

        private static void RunServer(HttpListener server, int managerId, IRuntime runtime, CancellationToken cancellation)
        {
            ThreadStart method = () => RunServerThreadProc(server, managerId, runtime, cancellation);
            Thread thread = new Thread(method);
            thread.Start();
        }

        private static void RunServerThreadProc(HttpListener server, int managerId, IRuntime runtime, CancellationToken cancellation)
        {
            try
            {
                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();
                    Task<HttpListenerContext> contextTask = server.GetContextAsync();
                    contextTask.Wait(cancellation);
                    HttpListenerContext context = contextTask.Result;
                    runtime.SendMessage(managerId, HttpActors.RequestArrived, context, 0);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled by user.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
