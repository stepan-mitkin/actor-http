using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// An autonomous actor that can accept and send asyncronous messages.
    /// The runtime guarantees to call all methods of an IActor in the same thread.
    /// </summary>
    public interface IActor
    {
        /// <summary>
        /// Reacts to a message sent to the actor.
        /// Must not block.
        /// </summary>
        /// <param name="runtime">The runtime this actor is living inside.</param>
        /// <param name="myActorId">The id of this actor.</param>
        /// <param name="message">The message. Not null.</param>
        void OnMessage(IRuntime runtime, int myActorId, Message message);

		/// <summary>
		/// The runtime will call this method before the actor is being destroyed.
		/// Should release owned resources.
		/// This method will be called only once.
		/// </summary>
		/// <param name="runtime">Runtime.</param>
		/// <param name="myActorId">My actor identifier.</param>
		void CleanUp(IRuntime runtime, int myActorId);
    }
}
