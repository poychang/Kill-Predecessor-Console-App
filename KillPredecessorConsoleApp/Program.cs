using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace KillPredecessorConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            Console.WriteLine($"Start process...{processName}");
            var predecessor = QueryPredecessorProcess(processName).ToList();

            PrintProcess(predecessor);
            ForceKillProcess(predecessor);

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
                Task.Delay(10 * 1000);
            } while (exit);
        }

        private static IEnumerable<ProcInfoDto> QueryPredecessorProcess(string processName)
        {
            return FetchProcInfoByCIMV2()
                .Where(p => p.Value.Name.StartsWith(processName))
                .Select(p => p.Value)
                .OrderBy(dto => dto.ElapsedTime)
                .Skip(1);
        }

        private static void PrintProcess(IEnumerable<ProcInfoDto> processes)
        {
            Console.WriteLine("PID\t\tName\t\tElapsed Time (Secs.)");
            Console.WriteLine("------------------------------------------------------");
            foreach (var process in processes)
            {
                Console.WriteLine($"{process.ProcessId}\t\t{process.Name}\t\t{process.ElapsedTime}");
            }
            Console.WriteLine();
        }

        private static void ForceKillProcess(IEnumerable<ProcInfoDto> processes)
        {
            try
            {
                foreach (var process in processes)
                {
                    var proc = Process.GetProcessById(process.ProcessId);
                    proc.Kill();
                    Console.WriteLine($"Process id: {proc.Id} has been killed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kill process failed, Message: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
        }

        private static Dictionary<int, ProcInfoDto> FetchProcInfoByCIMV2()
        {
            // http://wutils.com/wmi/root/cimv2/win32_perfformatteddata_perfproc_process/
            return new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process")
                .Get()
                .Cast<ManagementObject>()
                .Select(queryObj =>
                {
                    var pid = Convert.ToInt32(queryObj["IDProcess"]);
                    if (pid == 0) return null;
                    return new ProcInfoDto
                    {
                        ProcessId = pid,
                        Name = queryObj["Name"].ToString(),
                        ElapsedTime = Convert.ToUInt64(queryObj["ElapsedTime"])
                    };
                })
                .Where(p => p != null)
                .ToDictionary(p => p.ProcessId, o => o);
        }
    }

    public class ProcInfoDto
    {
        public int ProcessId { get; set; }
        public string Name { get; set; }
        public ulong ElapsedTime { get; set; }
    }
}
