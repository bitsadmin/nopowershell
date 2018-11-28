using System.Collections.Generic;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Text.RegularExpressions;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetSystemInfo : PSCommand
    {
        public GetSystemInfo(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            ResultRecord wmiOS = WmiHelper.ExecuteWmiQuery("Select * From Win32_OperatingSystem")[0];
            ResultRecord wmiCS = WmiHelper.ExecuteWmiQuery("Select * From Win32_ComputerSystem")[0];

            // OS Version
            string strOsVersion = string.Format("{0} Build {1}",
                wmiOS["Version"],
                /* wmiInfo["CSDVersion"],*/ // TODO
                wmiOS["BuildNumber"]);

            // Original Install Date
            Regex dateRegex = new Regex("([0-9]{4})([01][0-9])([012][0-9])([0-9]{2})([0-9]{2})([0-9]{2})");
            Match dateMatch = dateRegex.Matches(wmiOS["InstallDate"])[0];
            string sOrigInstallDate = string.Format("{0}-{1}-{2}, {3}:{4}:{5}",
                dateMatch.Groups[3], dateMatch.Groups[2], dateMatch.Groups[1],
                dateMatch.Groups[4], dateMatch.Groups[5], dateMatch.Groups[6]);

            // System Boot Time
            dateMatch = dateRegex.Matches(wmiOS["LastBootUpTime"])[0];
            string sSystemBootTime = string.Format("{0}-{1}-{2}, {3}:{4}:{5}",
                dateMatch.Groups[3], dateMatch.Groups[2], dateMatch.Groups[1],
                dateMatch.Groups[4], dateMatch.Groups[5], dateMatch.Groups[6]);

            // Processors
            CommandResult wmiCPUs = WmiHelper.ExecuteWmiQuery("Select * From Win32_Processor");
            List<string> cpus = new List<string>(wmiCPUs.Count);
            foreach(ResultRecord cpu in wmiCPUs)
                cpus.Add(string.Format("{0} ~{1} Mhz", cpu["Description"], cpu["CurrentClockSpeed"]));

            // Bios
            ResultRecord wmiBios = WmiHelper.ExecuteWmiQuery("Select * From Win32_BIOS")[0];
            dateMatch = dateRegex.Matches(wmiBios["ReleaseDate"])[0];
            string strBiosVersion = string.Format("{0} {1}, {2}-{3}-{4}",
                wmiBios["Manufacturer"], wmiBios["SMBIOSBIOSVersion"],
                dateMatch.Groups[3], dateMatch.Groups[2], dateMatch.Groups[1]);

            // Hotfixes
            CommandResult wmiHotfixes = WmiHelper.ExecuteWmiQuery("Select HotFixID From Win32_QuickFixEngineering");
            List<string> hotfixes = new List<string>(wmiHotfixes.Count);
            foreach(ResultRecord hotfix in wmiHotfixes)
                hotfixes.Add(hotfix["HotFixID"]);

            // Time zone
            int timeZone = Convert.ToInt32(wmiOS["CurrentTimeZone"]) / 60;
            string sTimeZone = string.Format("UTC{0}{1}", timeZone > 0 ? "+" : "-", timeZone);

            // Pagefile
            string sPageFile = WmiHelper.ExecuteWmiQuery("Select Name From Win32_PageFileUsage")[0]["Name"];

            // Summarize information
            _results.Add(
                new ResultRecord()
                {
                    { "Host Name", wmiOS["CSName"] },
                    { "OS Name", wmiOS["Caption"] },
                    { "OS Version", strOsVersion },
                    { "OS Manufacturer", wmiOS["Manufacturer"] },
                    { "OS Build Type", wmiOS["BuildType"] },
                    { "Registered Owner", wmiOS["RegisteredUser"] },
                    { "Registered Organization", wmiOS["Organization"] },
                    { "Product ID", wmiOS["SerialNumber"] },
                    { "Original Install Date", sOrigInstallDate },
                    { "System Boot Time", sSystemBootTime },
                    { "System Manufacturer", wmiCS["Manufacturer"] },
                    { "System Model", wmiCS["Model"] },
                    { "System Type", wmiCS["SystemType"] },
                    { "Processor(s)", string.Join(", ", cpus.ToArray()) },
                    { "BIOS Version", strBiosVersion },
                    { "Windows Directory", wmiOS["WindowsDirectory"] },
                    { "System Directory", wmiOS["SystemDirectory"] },
                    { "Boot Device", wmiOS["BootDevice"] },
                    { "System Locale", wmiOS["OSLanguage"] },
                    { "Input Locale", wmiOS["OSLanguage"] }, // TODO
                    { "Time Zone", sTimeZone}, // TODO
                    { "Total Physical Memory", wmiOS["TotalVisibleMemorySize"] },
                    { "Available Physical Memory", wmiOS["FreePhysicalMemory"] },
                    { "Virtual Memory: Max Size", wmiOS["TotalVirtualMemorySize"] },
                    { "Virtual Memory: Available", wmiOS["FreeVirtualMemory"] },
                    { "Virtual Memory: In Use", "" }, // TODO
                    { "Page File Location(s)", sPageFile },
                    { "Domain", wmiCS["Domain"] },
                    { "Logon Server", "" }, // TODO
                    { "Hotfix(s)", string.Join(", ", hotfixes.ToArray()) },
                    { "Network Card(s)", "" }, // TODO
                    { "Hyper-V Requirements", "" } // TODO
                }
            );
            
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-SystemInfo", "systeminfo" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Shows details about the system such as hardware and Windows installation."; }
        }
    }
}
