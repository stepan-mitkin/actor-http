using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActorGui
{
    public interface IMainWindow
    {
        void ReportError(string message);
        void ReportResult(string result);
        void SwitchToWorking();
        void SwitchToReady();
    }
}
