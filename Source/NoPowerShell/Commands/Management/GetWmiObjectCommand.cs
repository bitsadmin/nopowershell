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
    public class GetWmiObjectCommand : PSCommand
    {
        public GetWmiObjectCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters for remote execution
            base.Execute();

            // Obtain parameters
            string wmiNamespace = _arguments.Get<StringArgument>("Namespace").Value;
            string wmiQuery = _arguments.Get<StringArgument>("Query").Value;
            string wmiClass = _arguments.Get<StringArgument>("Class").Value;
            string wmiFilter = _arguments.Get<StringArgument>("Filter").Value;

            // If no query is specified, assume a class is specified
            if (wmiQuery != null && !wmiQuery.ToUpperInvariant().Contains("SELECT"))
                wmiClass = wmiQuery;

            if (wmiClass != null)
                wmiQuery = string.Format("Select * From {0}", wmiClass);
            if (wmiFilter != null)
                wmiQuery += string.Format(" Where {0}", wmiFilter);

            // Execute user provided WMI query
            _results = WmiHelper.ExecuteWmiQuery(wmiNamespace, wmiQuery, computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-WmiObject", "gwmi" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Namespace", @"ROOT\CIMV2"),
                    new StringArgument("Query"),
                    new StringArgument("Class", true),
                    new StringArgument("Filter", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets instances of WMI classes or information about the available classes."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "List local shares",
                        new List<string>()
                        {
                            "Get-WmiObject -Namespace ROOT\\CIMV2 -Query \"Select * From Win32_Share Where Name LIKE '%$'\"",
                            "gwmi -Class Win32_Share -Filter \"Name LIKE '%$'\""
                        }
                    ),
                    new ExampleEntry
                    (
                        "Obtain data of Win32_Process class from a remote system and apply a filter on the output",
                        new List<string>()
                        {
                            "Get-WmiObject \"Select ProcessId,Name,CommandLine From Win32_Process\" -ComputerName dc01.corp.local -Username MyUser -Password MyPassword | ? Name -Like *PowerShell* | select ProcessId,CommandLine",
                            "gwmi \"Select ProcessId,Name,CommandLine From Win32_Process\" -ComputerName dc01.corp.local | ? Name -Like *PowerShell* | select ProcessId,CommandLine"
                        }
                    ),
                    new ExampleEntry("View details about a certain service", "Get-WmiObject -Class Win32_Service -Filter \"Name = 'WinRM'\"")
                };
            }
        }
    }
}
