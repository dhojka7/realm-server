using RotMG.Common;
using RotMG.Game;
using RotMG.Networking;
using RotMG.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace RotMG
{
    public class Program
    {
        private static bool Terminating;
        private static int MainThread;
        private static ConcurrentQueue<Work> PendingWork;

        public static void Main(string[] args)
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
            PendingWork = new ConcurrentQueue<Work>();
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            Settings.Init();
            Resources.Init();
            Database.Init();
            AppServer.Init();
            GameServer.Init();
            Manager.Init();

            ThreadUtils.StartNewThread(ThreadPriority.Lowest, AppServer.Start);
            ThreadUtils.StartNewThread(ThreadPriority.Lowest, GameServer.Start);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Terminate);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Terminate);

            while (!Terminating)
            {
                while (PendingWork.TryDequeue(out Work work))
                {
                    try
                    {
                        work.Request();
                        work.Callback?.Invoke();
                    }
#if DEBUG
                    catch (Exception e1)
#endif
#if RELEASE
                    catch
#endif
                    {
#if DEBUG
                        Print(PrintType.Error, e1.ToString());
#endif
                        try
                        {
                            work.Callback?.Invoke();
                        }
#if DEBUG
                        catch (Exception e2)
                        {
                            Print(PrintType.Error, e2.ToString());
                        }
#endif
#if RELEASE
                        catch { }
#endif
                    }
                }

                Database.Tick();
                Manager.Tick();

#if DEBUG
                Thread.Sleep(2);
#endif
            }

            Terminate(null, null);
        }

        public static void Terminate(object sender, EventArgs e)
        {
            StartTerminating();
            Thread.Sleep(200);
            foreach (Client c in Manager.Clients.Values.ToArray())
            {
                try { c.Disconnect(); }
                catch { }
            }
            Thread.Sleep(200);
            try
            {
                AppServer.Stop();
                GameServer.Stop();
            }
            catch { }
        }

        public static void StartTerminating()
        {
            Terminating = true;
        }

        public static void PushWork(Action request, Action callback = null)
        {
            PendingWork.Enqueue(new Work
            {
                Request = request,
                Callback = callback
            });
        }

        public static void Print(PrintType type, object data)
        {
#if RELEASE
            if (type == PrintType.Debug)
                return;
#endif
            string message = $"<{DateTime.Now.ToShortTimeString()}> {data}";
            PushWork(() => 
            {
                switch (type)
                {
                    case PrintType.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                    case PrintType.Info:
                        break;
                    case PrintType.Warn:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case PrintType.Error:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.BackgroundColor = ConsoleColor.Red;
                        break;
                }
                Console.Write(message);
                Console.ResetColor();
                Console.WriteLine();
            });
        }
    }

    public enum PrintType
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public struct Work
    {
        public Action Request;
        public Action Callback;
    }
}
