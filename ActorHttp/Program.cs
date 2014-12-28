using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Linq;
using Actors;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace ActorHttp
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            int port;
            string folder;

            if (!ReadConfiguration(args, out port, out folder))
            {
                return;
            }

            try
            {
                using (MiniServer server = new MiniServer(port, folder))
                {
                    server.Start();
                    Console.WriteLine("Press 'Enter' to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Finished.");
        }

        private static bool ReadConfiguration(string[] args, out int port, out string folder)
        {
            port = 0;
            folder = null;
            if (args.Length == 0)
            {
                port = 8080;
                folder = ".";
            }
            else if (args.Length != 2)
            {
                Console.WriteLine("Actor HTTP megaserver. Usage:");
                Console.WriteLine("ActorHttp <port> <folder>");
                return false;
            }
            else
            {
                if (!Int32.TryParse(args[0], out port))
                {
                    Console.WriteLine("Port is not an integer.");
                    return false;
                }

                if (port <= 0 || port >= UInt16.MaxValue)
                {
                    Console.WriteLine("Bad port.");
                    return false;
                }

                folder = args[1];

                if (!Directory.Exists(folder))
                {
                    Console.WriteLine("Folder '{0}' does not exist.", folder);
                    return false;
                }
            }

            return true;
        }
    }
}
