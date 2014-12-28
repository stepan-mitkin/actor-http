using System;

namespace Actors
{
    /// <summary>
    /// Data for IO operations.
    /// </summary>
	public class IoBuffer
	{
        /// <summary>
        /// Contains the data to be written or the data that has been read.
        /// </summary>
		public byte[] Data = new byte[4096];

        /// <summary>
        /// The actual number of meaningful bytes in the Data field.
        /// </summary>
		public int Count = 0;
	}
}

