using System;
using System.Collections.Generic;

namespace Actors
{
    /// <summary>
    /// Codes for results of runtime function calls.
    /// </summary>
    public static class CallResult
    {
        public const int Completed = 1;
        public const int Error = 2;
        public const int Cancelled = 3;
        public const int Timeout = 4;

        public static void Check(int code)
        {
            if (code != Completed && code != Error && code != Cancelled && code != Timeout)
            {
                throw new InvalidOperationException("Bad call result.");
            }
        }
    }
}

