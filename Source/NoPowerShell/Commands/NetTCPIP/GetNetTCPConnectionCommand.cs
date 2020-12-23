using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetNetTCPConnectionCommand : PSCommand
    {
        public GetNetTCPConnectionCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect the (optional) ComputerName, Username and Password parameters
            base.Execute();

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
                    "Get-GetNetTCPConnection",
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
