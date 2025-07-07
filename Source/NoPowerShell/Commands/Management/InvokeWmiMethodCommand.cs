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
    public class InvokeWmiMethodCommand : PSCommand
    {
        public InvokeWmiMethodCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters for remote execution
            base.Execute();

            // Obtain parameters
            string wmiNamespace = _arguments.Get<StringArgument>("Namespace").Value;
            string wmiClass = _arguments.Get<StringArgument>("Class").Value;
            string methodName = _arguments.Get<StringArgument>("Name").Value;
            string methodArguments = _arguments.Get<StringArgument>("ArgumentList").Value;

            // Invoke WMI method
            _results = WmiHelper.InvokeWmiMethod(wmiNamespace, wmiClass, methodName, methodArguments, computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Invoke-WmiMethod", "iwmi" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name"),
                    new StringArgument("Namespace", @"root\cimv2"),
                    new StringArgument("Class"),
                    new StringArgument("ArgumentList")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Calls WMI methods."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Launch process", "Invoke-WmiMethod -Class Win32_Process -Name Create \"cmd /c calc.exe\""),
                    new ExampleEntry
                    (
                        "Launch process on remote system",
                        new List<string>()
                        {
                            "Invoke-WmiMethod -ComputerName MyServer -Username MyUserName -Password MyPassword -Class Win32_Process -Name Create \"powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA==\"",
                            "iwmi -ComputerName MyServer -Class Win32_Process -Name Create \"powershell -NoP -W H -E ZQBjAGgAbwAgACcASABlAGwAbABvACAATgBvAFAAbwB3AGUAcgBTAGgAZQBsAGwAIQAnAA==\""
                        }
                    ),
                };
            }
        }
    }
}
