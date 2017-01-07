using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.ComponentModel
{
    public static class BackgroundWorkerExtensions
    {
        public static void Wait(this BackgroundWorker worker)
        {
            while (worker.IsBusy)
            {
                Thread.Sleep(1);
                //Application.DoEvents();
            }
        }
        public static void Cancel(this BackgroundWorker worker)
        {
            if (worker.IsBusy)
                worker.CancelAsync();
            worker.Wait();
        }

        public static void ReportProgress(this BackgroundWorker worker, object progressState)
        {
            worker.ReportProgress((int)progressState);
        }
        public static void ReportProgress(this BackgroundWorker worker, object progressState, object userState)
        {
            worker.ReportProgress((int)progressState, userState);
        }
    }
}
