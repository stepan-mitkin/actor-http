using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// Objects of this type can be sent from one IActor to another.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets the type of the message, understood be the target actor.
        /// </summary>
		public readonly int Code;

		/// <summary>
		/// Optional call id. Must not be 0 for function calls.
		/// </summary>
		public readonly int Id;

        /// <summary>
        /// The message payload. Can be null.
		/// Should be immutable (ideally).
        /// </summary>
		public readonly object Payload;

		/// <summary>
		/// The sender of the message.
		/// </summary>
		public readonly int Sender;

		public Message(int code, int id, object payload, int sender)
        {
            Code = code;
			Id = id;
            Payload = payload;
			Sender = sender;
        }

        public override string ToString()
        {
			return String.Format("Message Code={0} Id={1} Payload={2} Sender={3}", Code, Id, Payload, Sender);
        }
    }
}
