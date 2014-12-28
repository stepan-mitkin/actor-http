using System;

namespace Actors
{
    /// <summary>
    /// Logs to the console.
    /// </summary>
    public class ConsoleLogger : IActorLogger
    {
        public void Error(string message, Exception ex)
        {
            Console.WriteLine("Error: {0}", message);
            if (ex != null)
            {
                Console.WriteLine(ex);
            }
        }
        public void Info(string message)
        {
            Console.WriteLine("{0}", message);
        }
    }
}

