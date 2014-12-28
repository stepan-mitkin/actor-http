using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actors
{
    internal struct Parcel
    {
        public int ActorId;
        public IActor Actor;
        public Message Message;
        public bool Kill;
    }
}
