using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.ComponentModel;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.DnsClient
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
            string type = _arguments.Get<StringArgument>("Type").Value;

            try
            {
                _results = DnsHelper.GetRecords(query, type);
            }
            catch(Win32Exception e)
            {
                throw new NoPowerShellException(e.Message);
            }

            // Determine columns in results
            List<string> columns = new List<string>();
            foreach (ResultRecord r in _results)
                foreach (string c in r.Keys)
                    if (!columns.Contains(c))
                        columns.Add(c);

            // Add missing columns
            foreach (ResultRecord r in _results)
            {
                foreach (string c in columns)
                {
                    if (!r.ContainsKey(c))
                        r.Add(c, null);
                }
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
                    new StringArgument("Name"),
                    new StringArgument("Type", "A")
                    // TODO:
                    //new StringArgument("Server", true),
                };
            }
        }

        public static new string Synopsis
        {
            get
            {
                return string.Format("Resolve DNS name.");
            }
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
                            "host linux.org",
                        }
                    ),
                    new ExampleEntry("Lookup specific record", "Resolve-DnsName -Type MX pm.me"),
                    new ExampleEntry("Reverse DNS lookup", "Resolve-DnsName 1.1.1.1")
                };
            }
        }
    }
}