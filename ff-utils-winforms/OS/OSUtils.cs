﻿using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System;
using System.Diagnostics;
using System.Management;
using DiskDetector;
using DiskDetector.Models;
using Nmkoder.IO;
using Tulpep.NotificationWindow;
using System.Threading.Tasks;
using Nmkoder.Utils;
using System.Linq;
using Microsoft.VisualBasic.Devices;
using System.Windows.Forms;
using Nmkoder.Extensions;

namespace Nmkoder.OS
{
    class OsUtils
    {
        public static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception e)
            {
                Logger.Log("IsUserAdministrator() Error: " + e.Message);
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }

        public static Process SetStartInfo(Process proc, bool hidden, string filename = "cmd.exe")
        {
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = hidden;
            proc.StartInfo.RedirectStandardError = hidden;
            proc.StartInfo.CreateNoWindow = hidden;
            proc.StartInfo.FileName = filename;
            return proc;
        }

        public static bool IsProcessHidden(Process proc)
        {
            bool defaultVal = true;

            try
            {
                if (proc == null)
                {
                    Logger.Log($"IsProcessHidden was called but proc is null, defaulting to {defaultVal}", true);
                    return defaultVal;
                }

                if (proc.HasExited)
                {
                    Logger.Log($"IsProcessHidden was called but proc has already exited, defaulting to {defaultVal}", true);
                    return defaultVal;
                }

                ProcessStartInfo si = proc.StartInfo;
                return !si.UseShellExecute && si.CreateNoWindow;
            }
            catch (Exception e)
            {
                Logger.Log($"IsProcessHidden errored, defaulting to {defaultVal}: {e.Message}", true);
                return defaultVal;
            }
        }

        public static Process NewProcess(bool hidden, NmkoderProcess.ProcessType type, string filename = "cmd.exe")
        {
            NmkoderProcess p = new NmkoderProcess(new Process(), type);
            ProcessManager.RegisterProcess(p);
            return SetStartInfo(p.Process, hidden, filename);
        }

        public static void KillProcessTree(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessTree(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }

        public static string GetCmdArg()
        {
            bool stayOpen = Config.GetInt(Config.Key.CmdDebugMode) == 2;
            if (stayOpen)
                return "/K";
            else
                return "/C";
        }

        public static bool ShowHiddenCmd()
        {
            return Config.GetInt(Config.Key.CmdDebugMode) > 0;
        }

        public static bool DriveIsSSD(string path)
        {
            try
            {
                var detectedDrives = Detector.DetectFixedDrives(QueryType.SeekPenalty);
                if (detectedDrives.Count != 0)
                {
                    char pathDriveLetter = (path[0].ToString().ToUpper())[0];
                    foreach (var detectedDrive in detectedDrives)
                    {
                        if (detectedDrive.DriveLetter == pathDriveLetter && detectedDrive.HardwareType.ToString().ToLower().Trim() == "ssd")
                            return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed to detect drive type: " + e.Message, true);
                return true;    // Default to SSD on fail
            }
            return false;
        }

        public static bool HasNonAsciiChars(string str)
        {
            return (Encoding.UTF8.GetByteCount(str) != str.Length);
        }

        public static int GetFreeRamMb()
        {
            try
            {
                return (int)(new ComputerInfo().AvailablePhysicalMemory / 1048576);
            }
            catch
            {
                return 1000;
            }
        }

        public static string TryGetOs()
        {
            string info = "";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    ManagementObjectCollection information = searcher.Get();

                    if (information != null)
                    {
                        foreach (ManagementObject obj in information)
                            info = $"{obj["Caption"]} | {obj["OSArchitecture"]}";
                    }

                    info = info.Replace("NT 5.1.2600", "XP").Replace("NT 5.2.3790", "Server 2003");
                }
            }
            catch (Exception e)
            {
                Logger.Log("TryGetOs Error: " + e.Message, true);
            }

            return info;
        }

        public static IEnumerable<Process> GetChildProcesses(Process process)
        {
            List<Process> children = new List<Process>();
            ManagementObjectSearcher mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));

            foreach (ManagementObject mo in mos.Get())
            {
                children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
            }

            return children;
        }

        public static async Task<string> GetOutputAsync(Process process, bool onlyLastLine = false)
        {
            Logger.Log($"Getting output for {process.StartInfo.FileName} {process.StartInfo.Arguments}", true);
            NmkdStopwatch sw = new NmkdStopwatch();

            Stopwatch timeSinceLastOutput = new Stopwatch();
            timeSinceLastOutput.Restart();

            string output = "";

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => output += $"{e.Data}\n";
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => output += $"{e.Data}\n";
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            while (!process.HasExited) await Task.Delay(50);
            while (timeSinceLastOutput.ElapsedMilliseconds < 100) await Task.Delay(50);
            output = output.Trim('\r', '\n');

            Logger.Log($"Output (after {sw}):  {output.Replace("\r", " / ").Replace("\n", " / ").Trunc(250)}", true);

            if (onlyLastLine)
                output = output.SplitIntoLines().LastOrDefault();

            return output;
        }

        public static void Shutdown()
        {
            Process proc = NewProcess(true, NmkoderProcess.ProcessType.Secondary);
            proc.StartInfo.Arguments = "/C shutdown -s -t 0";
            proc.Start();
        }

        public static void Hibernate()
        {
            Application.SetSuspendState(PowerState.Hibernate, true, true);
        }

        public static void Sleep()
        {
            Application.SetSuspendState(PowerState.Suspend, true, true);
        }

        public static void ShowNotification(string title, string text)
        {
            var popupNotifier = new PopupNotifier { TitleText = title, ContentText = text, IsRightToLeft = false };
            popupNotifier.BodyColor = System.Drawing.ColorTranslator.FromHtml("#323232");
            popupNotifier.ContentColor = System.Drawing.Color.White;
            popupNotifier.TitleColor = System.Drawing.Color.LightGray;
            popupNotifier.GradientPower = 0;
            popupNotifier.Popup();
        }

        public static void ShowNotificationIfInBackground(string title, string text)
        {
            if (Program.mainForm.IsInFocus())
                return;

            ShowNotification(title, text);
        }

        public static string GetPathVar(string additionalPath = null)
        {
            return GetPathVar(new[] { additionalPath });
        }

        public static string GetPathVar(IEnumerable<string> additionalPaths)
        {
            var paths = Environment.GetEnvironmentVariable("PATH").Split(';');
            List<string> newPaths = new List<string>();

            if (paths != null)
                newPaths.AddRange(additionalPaths.Where(p => p.IsNotEmpty()));

            newPaths.AddRange(paths.Where(x => x.Lower().Replace("\\", "/").StartsWith("c:/windows")).ToList());

            return string.Join(";", newPaths.Select(x => x.Replace("\\", "/"))) + ";";
        }
    }
}
