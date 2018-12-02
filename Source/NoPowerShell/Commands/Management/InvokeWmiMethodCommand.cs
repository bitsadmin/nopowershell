using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class InvokeWmiMethodCommand : PSCommand
    {
        public InvokeWmiMethodCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            string wmiNamespace = _arguments.Get<StringArgument>("Namespace").Value;
            string wmiClass = _arguments.Get<StringArgument>("Class").Value;
            string methodName = _arguments.Get<StringArgument>("Name").Value;
            string methodArguments = _arguments.Get<StringArgument>("ArgumentList").Value;

            // Remote parameters
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            _results = WmiHelper.InvokeWmiMethod(wmiNamespace, wmiClass, methodName, methodArguments, computerName, username, password);

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
                    new StringArgument("Namespace", @"root\cimv2", true),
                    new StringArgument("Class"),
                    new StringArgument("Name"),
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
                    new ExampleEntry("Launch process using WMI", "Invoke-WmiMethod -Class Win32_Process -Name Create \"cmd /c calc.exe\"")
                };
            }
        }
    }
}
