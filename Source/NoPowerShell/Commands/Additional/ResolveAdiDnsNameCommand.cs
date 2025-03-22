using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using static NoPowerShell.HelperClasses.DnsHelper;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class ResolveAdiDnsNameCommand : PSCommand
    {
        public ResolveAdiDnsNameCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain Username/Password parameters
            base.Execute(pipeIn);

            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string zonename = _arguments.Get<StringArgument>("ZoneName").Value;
            string name = _arguments.Get<StringArgument>("Name").Value;
            //string rrtype = _arguments.Get<StringArgument>("Type").Value;
            //DnsRecordType? recordType = ParseRecordType(rrtype);
            string ldapFilter = "(objectClass=dnsNode)";

            // Validate Type
            /*
            if (recordType == null)
            {
                string[] recordTypes = Enum.GetNames(typeof(DnsRecordType)).Select(typeName => typeName.Replace("DNS_TYPE_", string.Empty)).ToArray();

                throw new NoPowerShellException(
                    "Cannot validate argument on parameter 'Type'. The argument \"{0}\" does not belong to the set \"{1}\"",
                    rrtype,
                    string.Join(",", recordTypes)
                );
            }
            */

            // Create filter for Name if specified
            if (!string.IsNullOrEmpty(name))
                ldapFilter = $"(&{ldapFilter}(dc={name}))";

            // Obtain DN
            string distinguishedName = LDAPHelper.GetDistinguishedName(server, username, password);

            // Collect records
            string searchBase = string.Format("DC={0},CN=MicrosoftDNS,DC=DomainDnsZones,{1}", zonename, distinguishedName);

            // Query
            List<string> properties = new List<string>() { "distinguishedName", "whenCreated", "whenChanged", "dNSTombstoned", "dnsRecord" };
            _results = LDAPHelper.QueryLDAP(searchBase, SearchScope.OneLevel, ldapFilter, properties, 0, server, username, password);

            return _results;
        }

        /*
        private static DnsRecordType? ParseRecordType(string recordType)
        {
            string recordString = string.Format("DNS_TYPE_{0}", recordType.ToUpper());

            if (Enum.TryParse<DnsRecordType>(recordString, true, out DnsRecordType type))
            {
                return type;
            }

            return null;
        }
        */

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Resolve-AdiDnsName" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("ZoneName"),
                    //new StringArgument("Type", true),
                    new StringArgument("Name", true),
                    new StringArgument("Server", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Obtains DNS names via ADIDNS"; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Obtain IP address of host W11", "Resolve-AdiDnsName -ZoneName ad.bitsadmin.com -Name W11"),
                    new ExampleEntry("Obtain LDAP servers in domain", "Resolve-AdiDnsName -ZoneName ad.bitsadmin.com -Name _ldap._tcp")
                };
            }
        }
    }
}
