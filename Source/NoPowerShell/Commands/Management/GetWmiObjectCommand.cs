using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetWmiObjectCommand : PSCommand
    {
        public GetWmiObjectCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            string wmiNamespace = _arguments.Get<StringArgument>("Namespace").Value;
            string wmiQuery = _arguments.Get<StringArgument>("Query").Value;
            string wmiClass = _arguments.Get<StringArgument>("Class").Value;
            string wmiFilter = _arguments.Get<StringArgument>("Filter").Value;

            if (wmiClass != null)
                wmiQuery = string.Format("Select * From {0}", wmiClass);
            if (wmiFilter != null)
                wmiQuery += string.Format(" Where {0}", wmiFilter);

            // Remote parameters
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            // Execute user provided WMI query and return results to pipeline
            _results = WmiHelper.ExecuteWmiQuery(wmiNamespace, wmiQuery, computerName, username, password);
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
                    new StringArgument("Namespace", @"root\cimv2", true),
                    new StringArgument("Query"),
                    new StringArgument("Class", null, true),
                    new StringArgument("Filter", null, true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets instances of WMI classes or information about the available classes."; }
        }
    }
}
