using System;

namespace Actors
{
    /// <summary>
    /// Responds to unhandled exceptions in the actor's OnMessage method.
    /// </summary>
	public interface IErrorHandler
	{
        /// <summary>
        /// Handles an exception that was thrown inside IActor.OnMessage.
        /// Re-creates the actor if necessary.
        /// </summary>
        /// <param name="runtime">The runtime.</param>
        /// <param name="actorId">The id of the failed actor.</param>
        /// <param name="actor">The actor object.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>If null, the runtime does nothing. If not null, the runtime
        /// will replace the failed actor with the returned object.</returns>
		IActor OnError(IRuntime runtime, int actorId, IActor actor, Exception ex);
	}
}

