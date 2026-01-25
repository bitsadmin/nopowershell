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
    public class GetNetTCPConnectionCommand : PSCommand
    {
        public GetNetTCPConnectionCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            // Collect the (optional) ComputerName, Username and Password parameters
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            // Perform query
            _results = WmiHelper.ExecuteWmiQuery("Root\\StandardCimv2", "Select LocalAddress, LocalPort, OwningProcess, RemoteAddress, RemotePort, State From MSFT_NetTCPConnection", computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get
            {
                return new CaseInsensitiveList()
                {
                    "Get-NetTCPConnection",
                    "netstat" // unofficial
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("ComputerName", true),
                    new StringArgument("Username", true),
                    new StringArgument("Password", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets TCP connections."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Show TCP connections on the local machine",
                        new List<string>()
                        {
                            "Get-NetTCPConnection",
                            "netstat"
                        }
                    ),
                    new ExampleEntry("Show TCP connections on a remote machine", "Get-NetTCPConnection -ComputerName MyServer"),
                };
            }
        }
    }
}
