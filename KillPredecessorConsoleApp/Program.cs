using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace KillPredecessorConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            Console.WriteLine($"Start process...{processName}");
            var predecessor = TaskManager.QueryPredecessorProcesses(processName).ToList();

            TaskManager.PrintProcesses(predecessor);
            TaskManager.ForceKillProcess(predecessor).ConfigureAwait(true);

            Console.WriteLine("All predecessor is terminated.");

            NonStopRunning();
        }

        private static void NonStopRunning()
        {
            var exit = true;
            do
            {
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    if (eventArgs.SpecialKey != ConsoleSpecialKey.ControlC) return;
                    eventArgs.Cancel = true;
                    exit = false;
                };
                Console.WriteLine(DateTime.Now);
                Thread.Sleep(10 * 1000);
            } while (exit);
        }
    }
}
