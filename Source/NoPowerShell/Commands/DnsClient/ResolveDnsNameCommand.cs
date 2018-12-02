using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.Net;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class ResolveDnsNameCommand : PSCommand
    {
        public ResolveDnsNameCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string query = _arguments.Get<StringArgument>("Name").Value;
            //string queryType = _arguments.Get<StringArgument>("Type").Value;
            //string server = _arguments.Get<StringArgument>("Server").Value;

            // Resolve and store results
            IPHostEntry response = Dns.GetHostEntry(query);

            foreach(IPAddress ip in response.AddressList)
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", response.HostName },
                        { "IPAddress", ip.ToString() }
                    }
                );
            }
            
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get
            {
                return new CaseInsensitiveList()
                {
                    "Resolve-DnsName",
                    "nslookup", "host" // Not official
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name")
                    // TODO:
                    //new StringArgument("Type", true),
                    //new StringArgument("Server", true),
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Resolve DNS name."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Resolve domain name",
                        new List<string>()
                        {
                            "Resolve-DnsName microsoft.com",
                            "host linux.org"
                        }
                    )
                };
            }
        }
    }
}
