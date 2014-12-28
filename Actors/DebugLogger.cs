using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Actors
{
    /// <summary>
    /// Logs to debug output.
    /// </summary>
    public class DebugLogger : IActorLogger
    {
        public void Error(string message, Exception ex)
        {
            Debug.WriteLine("Error: {0}", message);
            if (ex != null)
            {
                Debug.WriteLine(ex);
            }
        }
        public void Info(string message)
        {
            Debug.WriteLine("{0}", message);
        }
    }
}
