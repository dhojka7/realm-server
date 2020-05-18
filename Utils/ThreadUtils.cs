using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RotMG.Utils
{
    public static class ThreadUtils
    {
        public static void StartNewThread(ThreadPriority priority, Action action)
        {
            Thread thread = new Thread(() => { action(); });
            thread.Priority = priority;
            thread.Start();
        }

        public static void StopCurrentThread()
        {
            Thread thread = Thread.CurrentThread;
            thread.Join();
        }
    }
}
