using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    /// <summary>
    /// A contract for concurrent multi-actor environment.
    /// </summary>
    public interface IRuntime : IDisposable
    {
		IActorLogger Log { get; }

        /// <summary>
        /// Adds an actor to the runtime.
        /// One actor must belong only to one runtime.
		/// An internal thread will be selected to host the actor.
        /// </summary>
        /// <param name="actor">The actor to add. Accepts ownership.</param>
        /// <returns>Returns the actor id.</returns>
        int AddActor(IActor actor);

        /// <summary>
        /// Adds an actor to the specified thread of the runtime.
        /// One actor must belong only to one runtime.
        /// </summary>
        /// <param name="threadName">The thread name to add the actor to.</param>
        /// <param name="actor">The actor to add. Accepts ownership.</param>
        /// <returns>Returns the actor id.</returns>
        int AddActorToThread(string threadName, IActor actor);

        /// <summary>
        /// Creates a dedicated thread and places the actor into it.
        /// The thread will send Codes.Pulse messages to the actor busily.
        /// </summary>
        /// <param name="actor">The actor to add. Accepts ownership.</param>
        /// <returns>Returns the actor id.</returns>
        int AddDedicatedActor(IActor actor);

        /// <summary>
        /// Removes an existing actor from the runtime.
        /// The actors's CleanUp method will be called.
        /// </summary>
        /// <param name="actorId">The actor id.</param>
        void RemoveActor(int actorId);

        /// <summary>
        /// Asyncronously sends a message to the actor.
        /// Returns immediately.
        /// </summary>
        /// <param name="actorId">The recepient actor id.</param>
		/// <param name="messageCode">Message code.</param>
		/// <param name="payload">Payload. Can be null.</param>
        /// <param name="sender">The id of the actor that sends the message (optional).</param>
        void SendMessage(int actorId, int messageCode, object payload, int sender);

        /// <summary>
        /// Asyncronously starts a function call against an actor.
        /// Returns immediately.
        /// </summary>
        /// <param name="actorId">The recepient actor id.</param>
        /// <param name="messageCode">Message code.</param>
        /// <param name="payload">Payload. Can be null.</param>
        /// <param name="sender">The caller that should receive the result of the call.</param>
        /// <param name="timeout">If no result comes from the target actor after this timeout, the caller actor will get CallResult.Timeout</param>
        /// <returns>Returns a unique call id.</returns>
        int SendCall(int actorId, int messageCode, object payload, int sender, TimeSpan timeout);

        /// <summary>
        /// Sends the result of a received function call.
        /// </summary>
        /// <param name="caller">The original sender of the function call.</param>
        /// <param name="callId">The unique call id.</param>
        /// <param name="resultCode">The result code. Must be one of CodeResult.</param>
        /// <param name="payload">Payload. Can be null.</param>
        /// <param name="sender">The current actor id.</param>
		void SendResult(int caller, int callId, int resultCode, object payload, int sender);

        /// <summary>
        /// Sends an CallResult.Timeout message to the actor after the specified time interval.
        /// Returns immediately.
        /// </summary>
        /// <param name="actorId">The target actor id.</param>
        /// <param name="duration">The specified time interval.</param>
        /// <returns>Returns a unique id of the timer object. The timer object
        /// will be destroyed automatically after the timeout.</returns>
        int SendTimeout(int actorId, TimeSpan duration);

        /// <summary>
        /// Cancels a previously scheduled timeout message and destroys the timer.
        /// </summary>
        /// <param name="timerId">The unique timer id.</param>
        void CancelTimeout(int timerId);

        /// <summary>
        /// Starts an asyncronous method as a runtime function call.
        /// </summary>
        /// <param name="method">The method to start.</param>
        /// <param name="actorId">The actor id that will return the result.</param>
        /// <returns>Returns the unique call id.</returns>
		int StartVoidCall(Func<Task> method, int actorId);

        /// <summary>
        /// Starts an asyncronous void-returning method as a runtime function call.
        /// </summary>
        /// <param name="method">The method to start.</param>
        /// <param name="actorId">The actor id that will return the result.</param>
        /// <returns>Returns the unique call id.</returns>
		int StartCall<T>(Func<Task<T>> method, int actorId);

		/// <summary>
		/// Starts an async stream write operation as a runtime function call.
		/// </summary>
		/// <param name="stream">Stream to write to.</param>
		/// <param name="buffer">The source buffer.</param>
		/// <param name="actorId">The actor that will receive the result.</param>
		/// <returns>Returns the unique call id.</returns>
        int StartWrite (Stream stream, IoBuffer buffer, int actorId);

        /// <summary>
        /// Starts an async stream read operation as a runtime function call.
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="actorId">The actor that will receive the result.</param>
        /// <returns>Returns the unique call id.</returns>
		int StartRead (Stream stream, IoBuffer buffer, int actorId);
    }
}
