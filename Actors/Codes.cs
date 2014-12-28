using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// Widely used generic message codes.
    /// </summary>
    public static class Codes
    {
        public const int Invalid = 0;
        public const int Start = 150;
        public const int Shutdown = 200;
        public const int Cancel = 300;
        public const int Pulse = 1010;

        public const int Max = 10000;
    }
}
