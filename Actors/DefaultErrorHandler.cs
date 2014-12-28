using System;

namespace Actors
{
    /// <summary>
    /// Logs the occured exception to the runtime logger.
    /// </summary>
    public class DefaultErrorHandler : IErrorHandler
    {
        public IActor OnError(IRuntime runtime, int actorId, IActor actor, Exception ex)
        {
            string message = String.Format("ActorId={0} of type={1}", actorId, actor.GetType().Name);
            runtime.Log.Error(message, ex);
            return null;
        }
    }
}

