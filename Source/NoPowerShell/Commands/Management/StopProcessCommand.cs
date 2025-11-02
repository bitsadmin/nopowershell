using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.ComponentModel;
using System.Diagnostics;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class StopProcessCommand : PSCommand
    {
        public StopProcessCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Process IDs
            string Ids = _arguments.Get<StringArgument>("Id").Value;
            string[] processIds = null;
            if (Ids != null)
                processIds = Ids.Split(',');

            // Process Names
            //string Names = _arguments.Get<StringArgument>("ProcessName").Value;
            //string[] processNames = null;
            //if (Ids != null)
            //    processNames = Names.Split(',');
            

            // Force flag
            bool force = _arguments.Get<BoolArgument>("Force").Value;

            // If no IDs/Names are provided via the commandline, check incoming pipe
            // In this case Get-Process can be used
            if (processIds == null)// && processNames == null)
            {
                processIds = new string[pipeIn.Count];
                int i = 0;
                foreach (ResultRecord r in pipeIn)
                {
                    processIds[i] = r["ProcessId"];
                    i++;
                }
            }

            // Stop if no process IDs/Names are provided
            if ((processIds == null || processIds.Length == 0))// && (processNames == null || processNames.Length == 0))
            {
                throw new NoPowerShellException("No process IDs provided nor input from the input pipe received.");
            }

            // Convert string array to int array
            int[] processIdsInt = Array.ConvertAll(processIds, int.Parse);

            // Shutdown/kill processes
            foreach (int processId in processIdsInt)
            {
                string processName = null;

                try
                {
                    Process processToKill = Process.GetProcessById(processId);
                    processName = processToKill.ProcessName;

                    // If process does not have a window, use Kill method
                    if (processToKill.MainWindowHandle == IntPtr.Zero)
                    {
                        processToKill.Kill();
                    }
                    // Process has a window
                    else
                    {
                        if (force)
                            processToKill.Kill();
                        else
                            processToKill.CloseMainWindow();
                    }
                }
                catch (ArgumentException ex)
                {
                    // Process already exited
                    if (ex.Message.Contains("is not running"))
                    {
                        throw new NoPowerShellException("Cannot find a process with the process identifier {0}.", processId);
                    }
                    else
                    {
                        throw new NoPowerShellException("Cannot stop process \"{0} ({1})\" because of the following error: {2}", processName, processId, ex.Message);
                    }
                }
                catch (Win32Exception ex)
                {
                    throw new NoPowerShellException("Cannot stop process \"{0} ({1})\" because of the following error: {2}", processName, processId, ex.Message);
                }
            }

            return null;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Stop-Process", "kill", "spps" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Id", true),
                    new StringArgument("ProcessName", true),
                    new BoolArgument("Force")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Stops one or more running processes."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Gracefully stop processes" , "Stop-Process -Id 4512,7241"),
                    new ExampleEntry("Kill process" , "Stop-Process -Force -Id 4512"),
                    new ExampleEntry("Kill all cmd.exe processes", "Get-Process cmd | Stop-Process -Force")
                };
            }
        }
    }
}
