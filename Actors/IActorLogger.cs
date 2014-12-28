using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// A simple logger contract.
    /// </summary>
    public interface IActorLogger
    {
        void Error(string message, Exception ex);
        void Info(string message);
    }
}
