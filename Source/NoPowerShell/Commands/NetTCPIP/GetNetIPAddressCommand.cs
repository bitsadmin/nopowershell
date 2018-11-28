using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetNetIPAddress : PSCommand
    {
        public GetNetIPAddress(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            bool all = _arguments.Get<BoolArgument>("All").Value;

            string simpleSelect = "Description, IPAddress, DefaultIPGateway";
            string allSelect = simpleSelect + ", DNSServerSearchOrder";
            string query = "Select {0} From Win32_NetworkAdapterConfiguration {1}";

            if (all)
                query = string.Format(query, allSelect, string.Empty);
            else
                query = string.Format(query, simpleSelect, "Where IPEnabled = 'True'");

            _results = WmiHelper.ExecuteWmiQuery(query);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get {
                return new CaseInsensitiveList()
                {
                    "Get-NetIPAddress", "ipconfig",
                    "ifconfig" // Not official
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("All")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the IP address configuration."; }
        }
    }
}
