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
        public GetNetIPAddress(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute(pipeIn);

            // Obtain Username/Password parameters
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

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

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-NetIPAddress",
            "ipconfig", // Not official
            "ifconfig" // Not official
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("ComputerName", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            new BoolArgument("All")
        };

        public static new string Synopsis => "Gets the IP address configuration.";

        public static new ExampleEntries Examples => new ExampleEntries()
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
            new ExampleEntry
            (
                "Show all network interfaces",
                new List<string>()
                {
                    "Get-NetIPAddress -All",
                    "ipconfig -All"
                }
            ),
            new ExampleEntry
            (
                "Show all network interfaces on a remote machine using WMI",
                new List<string>()
                {
                    "Get-NetIPAddress -All -ComputerName MyServer -Username MyUser -Password MyPassword",
                    "Get-NetIPAddress -All -ComputerName MyServer"
                }
            )
        };
    }
}
