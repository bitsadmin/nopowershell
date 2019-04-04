using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
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
            string Ids = _arguments.Get<StringArgument>("Id").Value;
            string[] processIds = null;
            if (Ids != null)
                processIds = Ids.Split(',');
            bool force = _arguments.Get<BoolArgument>("Force").Value;

            // If no IDs are provided via the commandline, check incoming pipe
            // In this case Get-Process can be used
            if(processIds == null)
            {
                processIds = new string[pipeIn.Count];
                int i = 0;
                foreach(ResultRecord r in pipeIn)
                {
                    processIds[i] = r["ProcessId"];
                    i++;
                }
            }

            // Convert string array to int array
            int[] processIdsInt = Array.ConvertAll(processIds, int.Parse);

            // Shutdown/kill processes
            foreach (int processId in processIdsInt)
            {
                Process processToKill = Process.GetProcessById(processId);

                if (force)
                    processToKill.Kill();
                else
                    processToKill.CloseMainWindow();
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
                    new StringArgument("Id"),
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
