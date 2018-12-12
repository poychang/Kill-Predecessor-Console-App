using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace KillPredecessorConsoleApp
{
    public static class TaskManager
    {
        /// <summary>
        /// 終止先前執行的相同程序
        /// </summary>
        public static void TerminatePredecessorProcess()
        {
            var processName = Process.GetCurrentProcess().ProcessName;
            var predecessors = QueryPredecessorProcesses(processName);
            ForceKillProcess(predecessors).ConfigureAwait(true);
        }

        /// <summary>
        /// 終止指定的程序
        /// </summary>
        /// <param name="processName">程序名稱</param>
        public static void TerminateProcess(string processName)
        {
            var processes = QueryProcesses(processName);
            ForceKillProcess(processes).ConfigureAwait(true);
        }

        /// <summary>
        /// 取得先前執行的相同程序
        /// </summary>
        /// <param name="processName">程序名稱</param>
        /// <returns></returns>
        internal static IEnumerable<ProcessInfo> QueryPredecessorProcesses(string processName)
        {
            return QueryProcesses(processName)
                .OrderBy(dto => dto.ElapsedTime)
                .Skip(1);
        }

        /// <summary>
        /// 取得執行中的程序清單
        /// </summary>
        /// <param name="processName">程序名稱</param>
        /// <returns></returns>
        internal static IEnumerable<ProcessInfo> QueryProcesses(string processName)
        {
            return FetchProcInfoByCIMV2()
                .Where(p => p.Value.Name.StartsWith(processName))
                .Select(p => p.Value);
        }

        /// <summary>
        /// 列出程序資訊
        /// </summary>
        /// <param name="processes">程序清單</param>
        internal static void PrintProcesses(IEnumerable<ProcessInfo> processes)
        {
            Debug.WriteLine("PID\t\tName\t\tElapsed Time (Secs.)");
            Debug.WriteLine("------------------------------------------------------");
            foreach (var process in processes)
            {
                Debug.WriteLine($"{process.ProcessId}\t\t{process.Name}\t\t{process.ElapsedTime}");
            }
        }

        /// <summary>
        /// 強制終止程序
        /// </summary>
        /// <param name="processes">程序列表</param>
        /// <returns></returns>
        internal static Task ForceKillProcess(IEnumerable<ProcessInfo> processes)
        {
            Debug.WriteLine($"{nameof(ForceKillProcess)} start");
            try
            {
                foreach (var process in processes)
                {
                    Process.GetProcessById(process.ProcessId).Kill();
                    Debug.WriteLine($"Process ID: {process.ProcessId} has been killed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Kill process failed, Message: {ex.Message}, StackTrace: {ex.StackTrace}");
            }
            Debug.WriteLine($"{nameof(ForceKillProcess)} end");

            return Task.CompletedTask;
        }

        /// <summary>
        /// 使用 Win32 API 進行 WMI 查詢，取得所有程序資訊
        /// </summary>
        /// <remarks>相關 WMI 查詢資訊請參考：http://wutils.com/wmi/root/cimv2/win32_perfformatteddata_perfproc_process/ </remarks>
        /// <returns></returns>
        private static Dictionary<int, ProcessInfo> FetchProcInfoByCIMV2()
        {
            return new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process")
                .Get()
                .Cast<ManagementObject>()
                .Select(managementObject =>
                {
                    var pid = Convert.ToInt32(managementObject["IDProcess"]);
                    if (pid == 0) return null;
                    return new ProcessInfo
                    {
                        ProcessId = pid,
                        Name = managementObject["Name"].ToString(),
                        ElapsedTime = Convert.ToUInt64(managementObject["ElapsedTime"])
                    };
                })
                .Where(p => p != null)
                .ToDictionary(p => p.ProcessId, p => p);
        }
    }

    /// <summary>
    /// 程序資訊
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// PID
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 程序名稱
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 運行時間
        /// </summary>
        public ulong ElapsedTime { get; set; }
    }
}
