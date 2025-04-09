using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetHotFixCommand : PSCommand
    {
        public GetHotFixCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters for remote execution
            base.Execute();

            CommandResult wmiHotfixes = WmiHelper.ExecuteWmiQuery("Select CSName,Description,HotFixID,InstalledBy,InstalledOn From Win32_QuickFixEngineering", computername, username, password);
            foreach (ResultRecord hotfix in wmiHotfixes)
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { "Source", hotfix["CSName"] },
                        { "Description", hotfix["Description"] },
                        { "HotFixID", hotfix["HotFixID"] },
                        { "InstalledBy", hotfix["InstalledBy"] },
                        { "InstalledOn", hotfix["InstalledOn"] }
                    }
                );
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-HotFix" }; }
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
            get { return "Gets the hotfixes that have been applied to the local and remote computers."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Get all hotfixes on the local computer", "Get-HotFix"),
                    new ExampleEntry
                    (
                        "Get all hotfixes from a remote computer using WMI",
                        new List<string>()
                        {
                            "Get-HotFix -ComputerName MyServer -Username Administrator -Password Pa$$w0rd",
                            "Get-HotFix -ComputerName MyServer"
                        }
                    )
                };
            }
        }
    }
}
