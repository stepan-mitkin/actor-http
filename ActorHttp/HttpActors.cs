// Autogenerated with DRAKON Editor 1.31
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Actors;

namespace ActorHttp {

public class HttpActors {
	internal const int RequestArrived = 840;
	internal const int PumpFinished = 841;
	internal const int ReadFolder = 842;


    public partial class StreamPump
        : IActor
    {
        // Framework
        public int MyId;
        public IRuntime Runtime;
        // Parameters
        internal int Manager; // The id of the pump manager.
        internal Stream InStream; // File stream that reads from disk.
        internal Stream OutStream; // HTTP stream that sends to the client.
        internal int TotalLength = 0; // The total length of the requested file.
        // Internal state
        internal IoBuffer InBuffer = new IoBuffer(); // Read data from disk here.
        internal IoBuffer OutBuffer = new IoBuffer(); // Send data to client from here.
        internal int SoFar = 0; // Number of bytes read so far.
        internal int Read; // Disk read read call id
        internal int Send; // Network send call id

        public enum StateNames {
            Destroyed,
            JustRead,
            ReadSend,
            JustSend,
            SendRemaining
        }

        private StateNames _state = StateNames.JustRead;

        public StateNames State {
            get { return _state; }
            internal set { _state = value; }
        }

        public const int CancelledMessage = CallResult.Cancelled;
        public const int CompletedMessage = CallResult.Completed;
        public const int ErrorMessage = CallResult.Error;

        public object OnMessage(int messageType, IRuntime runtime, int myId, Message message) {
            switch (messageType) {
                case CancelledMessage:
                    return Cancelled(runtime, myId, message);
                case CompletedMessage:
                    return Completed(runtime, myId, message);
                case ErrorMessage:
                    return Error(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object Cancelled(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.JustRead:
                    return JustRead_Cancelled(runtime, myId, message);
                case StateNames.ReadSend:
                    return ReadSend_Cancelled(runtime, myId, message);
                case StateNames.JustSend:
                    return JustSend_Cancelled(runtime, myId, message);
                case StateNames.SendRemaining:
                    return SendRemaining_Cancelled(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object Completed(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.JustRead:
                    return JustRead_Completed(runtime, myId, message);
                case StateNames.ReadSend:
                    return ReadSend_Completed(runtime, myId, message);
                case StateNames.JustSend:
                    return JustSend_Completed(runtime, myId, message);
                case StateNames.SendRemaining:
                    return SendRemaining_Completed(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object Error(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.JustRead:
                    return JustRead_Error(runtime, myId, message);
                case StateNames.ReadSend:
                    return ReadSend_Error(runtime, myId, message);
                case StateNames.JustSend:
                    return JustSend_Error(runtime, myId, message);
                case StateNames.SendRemaining:
                    return SendRemaining_Error(runtime, myId, message);
                default:
                    return null;
            }
        }

        private object JustRead_Completed(IRuntime runtime, int myId, Message message) {
            // item 344
            SaveReceivedCount(this, message);
            // item 343
            bool finished = SwapBuffers(this);
            // item 411
            StartSend(runtime, this, myId);
            // item 346
            if (finished) {
                // item 348
                State = StateNames.SendRemaining;
                return null;
            } else {
                // item 499
                StartRead(runtime, this, myId);
                // item 327
                State = StateNames.ReadSend;
                return null;
            }
        }

        private object JustRead_Error(IRuntime runtime, int myId, Message message) {
            // item 345
            Shutdown();
            return null;
        }

        private object JustRead_Cancelled(IRuntime runtime, int myId, Message message) {
            // item 345
            Shutdown();
            return null;
        }

        private object ReadSend_Completed(IRuntime runtime, int myId, Message message) {
            int _sw5030000_ = 0;
            // item 5030000
            _sw5030000_ = message.Id;
            // item 5030001
            if (_sw5030000_ == Read) {
                // item 398
                SaveReceivedCount(this, message);
                // item 333
                State = StateNames.JustSend;
                return null;
            } else {
                // item 5030002
                if (_sw5030000_ == Send) {
                    
                } else {
                    // item 5030003
                    throw new InvalidOperationException("Not expected:  " + _sw5030000_.ToString());
                }
                // item 376
                State = StateNames.JustRead;
                return null;
            }
        }

        private object ReadSend_Error(IRuntime runtime, int myId, Message message) {
            // item 365
            Shutdown();
            return null;
        }

        private object ReadSend_Cancelled(IRuntime runtime, int myId, Message message) {
            // item 365
            Shutdown();
            return null;
        }

        private object JustSend_Completed(IRuntime runtime, int myId, Message message) {
            // item 472
            bool finished = SwapBuffers(this);
            // item 523
            StartSend(runtime, this, myId);
            // item 403
            if (finished) {
                // item 405
                State = StateNames.SendRemaining;
                return null;
            } else {
                // item 524
                StartRead(runtime, this, myId);
                // item 408
                State = StateNames.ReadSend;
                return null;
            }
        }

        private object JustSend_Error(IRuntime runtime, int myId, Message message) {
            // item 385
            Shutdown();
            return null;
        }

        private object JustSend_Cancelled(IRuntime runtime, int myId, Message message) {
            // item 385
            Shutdown();
            return null;
        }

        private object SendRemaining_Completed(IRuntime runtime, int myId, Message message) {
            // item 470
            runtime.Log.Info(String.Format(
            	"Pump {0} completed request.",
            	myId
            ));
            // item 330
            Shutdown();
            return null;
        }

        private object SendRemaining_Error(IRuntime runtime, int myId, Message message) {
            // item 397
            Shutdown();
            return null;
        }

        private object SendRemaining_Cancelled(IRuntime runtime, int myId, Message message) {
            // item 397
            Shutdown();
            return null;
        }

        public void Shutdown() {
            if (State == StateNames.Destroyed) {
                return;
            }
            State = StateNames.Destroyed;
            // item 446
            Runtime.SendMessage(
            	Manager,
            	ServerConstants.PumpFinished,
            	MyId,
            	0
            );
            // item 349
            if (InStream == null) {
                
            } else {
                // item 352
                InStream.Dispose();
            }
            // item 353
            if (OutStream == null) {
                
            } else {
                // item 356
                OutStream.Dispose();
            }
        }
    }

    public partial class PumpManager
        : IActor
    {
        // Parameters
        public string Folder;
        public int MaxPumps;
        public int FolderReader;
        // State
        private readonly Dictionary<int, HttpListenerContext> ActivePumps =
        new Dictionary<int, HttpListenerContext>();

        public enum StateNames {
            Destroyed,
            Operational
        }

        private StateNames _state = StateNames.Operational;

        public StateNames State {
            get { return _state; }
            internal set { _state = value; }
        }

        public const int PumpFinishedMessage = ServerConstants.PumpFinished;
        public const int RequestArrivedMessage = ServerConstants.RequestArrived;

        public object OnMessage(int messageType, IRuntime runtime, int myId, Message message) {
            switch (messageType) {
                case PumpFinishedMessage:
                    return PumpFinished(runtime, myId, message);
                case RequestArrivedMessage:
                    return RequestArrived(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object PumpFinished(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Operational:
                    return Operational_PumpFinished(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object RequestArrived(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Operational:
                    return Operational_RequestArrived(runtime, myId, message);
                default:
                    return null;
            }
        }

        private object Operational_RequestArrived(IRuntime runtime, int myId, Message message) {
            // item 440
            HttpListenerContext context = (HttpListenerContext)message.Payload;
            // item 474
            string url = context.Request.RawUrl;
            // item 441
            if (ActivePumps.Count < MaxPumps) {
                // item 461
                runtime.Log.Info("Serving request for: " + url);
                // item 452
                int actorId = ProcessRequest(
                	runtime,
                	myId,
                	context,
                	Folder,
                	url,
                	FolderReader
                );
                // item 453
                ActivePumps.Add(actorId, context);
            } else {
                // item 460
                runtime.Log.Info("Limit reached. Aborting.");
                // item 443
                context.Response.Abort();
            }
            // item 425
            State = StateNames.Operational;
            return null;
        }

        private object Operational_PumpFinished(IRuntime runtime, int myId, Message message) {
            // item 457
            int actorId = (int)message.Payload;
            // item 454
            if (ActivePumps.ContainsKey(actorId)) {
                // item 464
                runtime.Log.Info("Closing pump: " + actorId);
                // item 458
                HttpListenerResponse response = ActivePumps[actorId].Response;
                ActivePumps.Remove(actorId);
                // item 459
                response.Close();
            } else {
                
            }
            // item 425
            State = StateNames.Operational;
            return null;
        }

        public void Shutdown() {
            if (State == StateNames.Destroyed) {
                return;
            }
            State = StateNames.Destroyed;
            
        }
    }

    public partial class IndexBuilder
        : IActor
    {
        // Framework
        public int MyId;
        public IRuntime Runtime;
        // Parameters
        internal int Manager; // The id of the pump manager.
        internal int FolderReader; // Folder reader id.
        internal string Folder; // The folder to enumerate.
        internal HttpListenerResponse Response;
        // Internal state
        internal int PumpId; // The stream pump id

        public enum StateNames {
            Destroyed,
            Init,
            WaitingForFolder,
            Sending
        }

        private StateNames _state = StateNames.Init;

        public StateNames State {
            get { return _state; }
            internal set { _state = value; }
        }

        public const int CancelledMessage = CallResult.Cancelled;
        public const int CompletedMessage = CallResult.Completed;
        public const int ErrorMessage = CallResult.Error;
        public const int TimeoutMessage = CallResult.Timeout;
        public const int StartMessage = Codes.Start;
        public const int PumpFinishedMessage = ServerConstants.PumpFinished;

        public object OnMessage(int messageType, IRuntime runtime, int myId, Message message) {
            switch (messageType) {
                case CancelledMessage:
                    return Cancelled(runtime, myId, message);
                case CompletedMessage:
                    return Completed(runtime, myId, message);
                case ErrorMessage:
                    return Error(runtime, myId, message);
                case TimeoutMessage:
                    return Timeout(runtime, myId, message);
                case StartMessage:
                    return Start(runtime, myId, message);
                case PumpFinishedMessage:
                    return PumpFinished(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object Cancelled(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Init:
                case StateNames.WaitingForFolder:
                    return WaitingForFolder_Cancelled(runtime, myId, message);
                case StateNames.Sending:
                default:
                    return null;
            }
        }

        public object Completed(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Init:
                case StateNames.WaitingForFolder:
                    return WaitingForFolder_Completed(runtime, myId, message);
                case StateNames.Sending:
                default:
                    return null;
            }
        }

        public object Error(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Init:
                case StateNames.WaitingForFolder:
                    return WaitingForFolder_Error(runtime, myId, message);
                case StateNames.Sending:
                default:
                    return null;
            }
        }

        public object Timeout(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Init:
                case StateNames.WaitingForFolder:
                    return WaitingForFolder_Timeout(runtime, myId, message);
                case StateNames.Sending:
                default:
                    return null;
            }
        }

        public object Start(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Init:
                    return Init_Start(runtime, myId, message);
                case StateNames.WaitingForFolder:
                case StateNames.Sending:
                default:
                    return null;
            }
        }

        public object PumpFinished(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Init:
                case StateNames.WaitingForFolder:
                case StateNames.Sending:
                    return Sending_PumpFinished(runtime, myId, message);
                default:
                    return null;
            }
        }

        private object Init_Start(IRuntime runtime, int myId, Message message) {
            // item 840
            runtime.SendCall(
            	FolderReader,
            	ReadFolder,
            	Folder,
            	myId,
            	TimeSpan.FromSeconds(1)
            );
            // item 766
            State = StateNames.WaitingForFolder;
            return null;
        }

        private object Init_default(IRuntime runtime, int myId, Message message) {
            // item 783
            State = StateNames.Init;
            return null;
        }

        private object WaitingForFolder_Completed(IRuntime runtime, int myId, Message message) {
            // item 843
            var files = (List<string>)message.Payload;
            // item 846
            string responseText = BuildFolderIndexPage(files);
            byte[] responseData = Encoding.UTF8.GetBytes(
            	responseText
            );
            // item 844
            PumpId = CreatePumpFromBytes(
            	runtime,
            	myId,
            	Response,
            	responseData,
            	200
            );
            // item 772
            State = StateNames.Sending;
            return null;
        }

        private object WaitingForFolder_Error(IRuntime runtime, int myId, Message message) {
            // item 801
            Shutdown();
            return null;
        }

        private object WaitingForFolder_Timeout(IRuntime runtime, int myId, Message message) {
            // item 900
            runtime.Log.Info(
            	"Timeout on ReadFolder"
            );
            // item 801
            Shutdown();
            return null;
        }

        private object WaitingForFolder_Cancelled(IRuntime runtime, int myId, Message message) {
            // item 801
            Shutdown();
            return null;
        }

        private object Sending_PumpFinished(IRuntime runtime, int myId, Message message) {
            // item 847
            int actorId = (int)message.Payload;
            // item 817
            if (actorId == PumpId) {
                // item 820
                Shutdown();
                return null;
            } else {
                // item 819
                State = StateNames.Sending;
                return null;
            }
        }

        private object Sending_default(IRuntime runtime, int myId, Message message) {
            // item 819
            State = StateNames.Sending;
            return null;
        }

        public void Shutdown() {
            if (State == StateNames.Destroyed) {
                return;
            }
            State = StateNames.Destroyed;
            // item 822
            Runtime.SendMessage(
            	Manager,
            	ServerConstants.PumpFinished,
            	MyId,
            	0
            );
        }
    }

    public partial class FolderReader
        : IActor
    {
        

        public enum StateNames {
            Destroyed,
            Working
        }

        private StateNames _state = StateNames.Working;

        public StateNames State {
            get { return _state; }
            internal set { _state = value; }
        }

        public const int ReadFolderMessage = ServerConstants.ReadFolder;

        public object OnMessage(int messageType, IRuntime runtime, int myId, Message message) {
            switch (messageType) {
                case ReadFolderMessage:
                    return ReadFolder(runtime, myId, message);
                default:
                    return null;
            }
        }

        public object ReadFolder(IRuntime runtime, int myId, Message message) {
            switch (State) {
                case StateNames.Working:
                    return Working_ReadFolder(runtime, myId, message);
                default:
                    return null;
            }
        }

        private object Working_ReadFolder(IRuntime runtime, int myId, Message message) {
            // item 870
            string folder = (string)message.Payload;
            // item 871
            List<string> files = null;
            Exception exception = null;
            try {
            	files = Directory
            		.EnumerateFiles(folder)
            		.ToList();
            } catch (Exception ex) {
            	exception = ex;
            	runtime.Log.Error(
            		"Error reading folder: " + folder,
            		ex
            	);
            }
            // item 873
            if (files == null) {
                // item 875
                runtime.SendResult(
                	message.Sender,
                	message.Id,
                	CallResult.Error,
                	exception,
                	myId
                );
            } else {
                // item 872
                runtime.SendResult(
                	message.Sender,
                	message.Id,
                	CallResult.Completed,
                	files,
                	myId
                );
            }
            // item 856
            State = StateNames.Working;
            return null;
        }

        private object Working_default(IRuntime runtime, int myId, Message message) {
            // item 856
            State = StateNames.Working;
            return null;
        }

        public void Shutdown() {
            if (State == StateNames.Destroyed) {
                return;
            }
            State = StateNames.Destroyed;
            
        }
    }

    public static int ProcessRequest(IRuntime runtime, int manager, HttpListenerContext context, string folder, string url, int reader) {
        // item 670
        int actorId;
        string path = folder + "/" + url;
        // item 674
        if (Directory.Exists(path)) {
            // item 901
            string indexPath = path + "/" + "index.html";
            // item 904
            if (File.Exists(indexPath)) {
                // item 905
                path = indexPath;
                // item 707
                actorId = CreateFilePump(
                	runtime,
                	path,
                	manager,
                	context.Response
                );
                // item 750
                if (actorId == 0) {
                    // item 753
                    actorId = Create404Pump(
                    	runtime,
                    	manager,
                    	context.Response
                    );
                } else {
                    
                }
            } else {
                // item 755
                actorId = CreateIndexBuilder(
                	runtime,
                	manager,
                	context.Response,
                	path,
                	reader
                );
            }
        } else {
            // item 707
            actorId = CreateFilePump(
            	runtime,
            	path,
            	manager,
            	context.Response
            );
            // item 750
            if (actorId == 0) {
                // item 753
                actorId = Create404Pump(
                	runtime,
                	manager,
                	context.Response
                );
            } else {
                
            }
        }
        // item 754
        return actorId;
    }

    private static string BuildFolderIndexPage(List<string> files) {
        // item 892
        StringBuilder sb = new StringBuilder();
        // item 894
        sb.AppendLine("<html>");
        sb.AppendLine("<body>");
        foreach (string file in files) {
            // item 899
            string filename = Path.GetFileName(file);
            // item 898
            sb.AppendFormat("<p><a href=\"{0}\">{0}</a></p>", filename);
            sb.AppendLine();
        }
        // item 895
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        // item 893
        return sb.ToString();
    }

    private static int Create404Pump(IRuntime runtime, int manager, HttpListenerResponse response) {
        // item 731
        byte[] responseData = Encoding.UTF8.GetBytes(
        	"<html><body>" +
        	"<h1>404 Not Found</h1>" +
        	"<p>Actor HTTP megaserver</p><p>Resource not found.</p>" +
        	"</body></html>"
        );
        // item 748
        return CreatePumpFromBytes(
        	runtime,
        	manager,
        	response,
        	responseData,
        	404
        );
    }

    private static int CreateFilePump(IRuntime runtime, string path, int manager, HttpListenerResponse response) {
        // item 697
        if (File.Exists(path)) {
            // item 698
            Stream fstream = TryOpenFile(path);
            // item 699
            if (fstream == null) {
                // item 703
                runtime.Log.Info(
                	"Could not open file: "
                	+ path
                );
                // item 701
                return 0;
            } else {
                // item 691
                FileInfo about = new FileInfo(path);
                // item 696
                runtime.Log.Info(
                	String.Format(
                		"Found file: {0}. Length: {1}", 
                		path,
                		(int)about.Length
                	)
                );
                // item 689
                var pump = new StreamPump();
                pump.Manager = manager;
                pump.TotalLength = (int)about.Length;
                pump.InStream = fstream;
                pump.OutStream = response.OutputStream;
                // item 690
                int id = runtime.AddActor(pump);
                pump.MyId = id;
                pump.Runtime = runtime;
                // item 692
                response.StatusCode = 200;
                response.ContentLength64 = pump.TotalLength;
                // item 694
                runtime.StartRead(
                	pump.InStream,
                	pump.InBuffer,
                	id
                );
                // item 695
                return id;
            }
        } else {
            // item 702
            runtime.Log.Info(
            	"File not found: "
            	+ path
            );
            // item 701
            return 0;
        }
    }

    private static int CreateIndexBuilder(IRuntime runtime, int manager, HttpListenerResponse response, string path, int reader) {
        // item 883
        var builder = new IndexBuilder();
        builder.Manager = manager;
        builder.FolderReader = reader;
        builder.Folder = path;
        builder.Response = response;
        // item 884
        int actorId = runtime.AddActor(builder);
        builder.MyId = actorId;
        builder.Runtime = runtime;
        // item 886
        runtime.SendMessage(
        	actorId,
        	Codes.Start,
        	null,
        	0
        );
        // item 885
        return actorId;
    }

    private static int CreatePumpFromBytes(IRuntime runtime, int manager, HttpListenerResponse response, byte[] responseData, int responseCode) {
        // item 744
        response.StatusCode = responseCode;
        response.ContentLength64 = responseData.Length;
        // item 740
        var pump = new StreamPump();
        pump.Manager = manager;
        pump.OutStream = response.OutputStream;
        pump.OutBuffer.Data = responseData;
        pump.OutBuffer.Count = responseData.Length;
        // item 745
        pump.State = StreamPump.StateNames.SendRemaining;
        // item 911
        int id = runtime.AddActor(pump);
        pump.MyId = id;
        pump.Runtime = runtime;
        // item 746
        runtime.StartWrite(
        	pump.OutStream,
        	pump.OutBuffer,
        	id
        );
        // item 742
        return id;
    }

    private static bool IsReadingFinished(StreamPump pump) {
        // item 266
        if ((pump.InBuffer.Count == 0) || (!(pump.SoFar < pump.TotalLength))) {
            // item 269
            return true;
        } else {
            // item 270
            return false;
        }
    }

    private static void SaveReceivedCount(StreamPump pump, Message message) {
        // item 262
        pump.InBuffer.Count = (int)message.Payload;
        pump.SoFar += pump.InBuffer.Count;
    }

    private static void StartRead(IRuntime runtime, StreamPump pump, int myId) {
        // item 491
        pump.Read = runtime.StartRead(
        	pump.InStream,
        	pump.InBuffer,
        	myId
        );
    }

    private static void StartSend(IRuntime runtime, StreamPump pump, int myId) {
        // item 498
        pump.Send = runtime.StartWrite(
        	pump.OutStream,
        	pump.OutBuffer,
        	myId
        );
    }

    private static bool SwapBuffers(StreamPump pump) {
        // item 471
        bool finished = IsReadingFinished(pump);
        // item 254
        IoBuffer oldIn = pump.InBuffer;
        IoBuffer oldOut = pump.OutBuffer;
        // item 255
        pump.OutBuffer = oldIn;
        pump.InBuffer = oldOut;
        // item 473
        return finished;
    }

    private static Stream TryOpenFile(string path) {
        // item 248
        try {
        	Stream fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
        	return fstream;
        }
        catch {
        	return null;
        }
    }
}
}
