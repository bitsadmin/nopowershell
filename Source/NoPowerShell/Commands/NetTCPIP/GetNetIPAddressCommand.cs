using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.NetTCPIP
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

            _results = WmiHelper.ExecuteWmiQuery(query, computername, username, password);

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

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Show network interfaces",
                        new List<string>()
                        {
                            "Get-NetIPAddress",
                            "ipconfig",
                            "ifconfig"
                        }
                    ),
                    new ExampleEntry("Show all network interfaces", "Get-NetIPAddress -All")
                };
            }
        }
    }
}
