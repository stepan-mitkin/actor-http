using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    internal interface IThread
    {
        string Name { get; }
        bool IsNormal { get; }
        void PostMessage(int actorId, Message message);
        void AddActor(int actorId, IActor actor);
        void RemoveActor(int actorId);
        void Start();
        void Stop();
    }
}
