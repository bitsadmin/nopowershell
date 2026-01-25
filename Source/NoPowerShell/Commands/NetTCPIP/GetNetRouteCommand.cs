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
    public class GetNetRouteCommand : PSCommand
    {
        public GetNetRouteCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            // Collect the (optional) ComputerName, Username and Password parameters
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            _results = WmiHelper.ExecuteWmiQuery("Select Caption, Description, Destination, Mask, NextHop From Win32_IP4RouteTable", computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-NetRoute",
            "route" // Not official
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("ComputerName", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true)
        };

        public static new string Synopsis => "Gets the IP route information from the IP routing table.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry
            (
                "Show the IP routing table",
                new List<string>()
                {
                    "Get-NetRoute",
                    "route"
                }
            ),
            new ExampleEntry
            (
                "Show the IP routing table on a remote machine using WMI",
                new List<string>()
                {
                    "Get-NetRoute -ComputerName MyServer -Username MyUser -Password MyPassword",
                    "route -ComputerName MyServer"
                }
            )
        };
    }
}
