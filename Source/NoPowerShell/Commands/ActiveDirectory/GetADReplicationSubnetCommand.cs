using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADReplicationSubnetCommand : PSCommand
    {
        public GetADReplicationSubnetCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain Username/Password parameters
            base.Execute(pipeIn);

            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            //string searchBase = _arguments.Get<StringArgument>("SearchBase").Value;
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            //string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            List<string> properties = new List<string>(_arguments.Get<StringArgument>("Properties").Value.Split(','));

            // Query
            string distinguishedName = LDAPHelper.GetDistinguishedName(server, username, password);

            if(string.IsNullOrEmpty(distinguishedName))
                throw new Exception("Could not determine the distinguished name of the domain");

            string searchBase = $"CN=Subnets,CN=Sites,CN=Configuration,{distinguishedName}";
            string filter = "(objectClass=subnet)";
            if (!string.IsNullOrEmpty(identity))
            {
                string identityEscaped = identity.Replace("/", "\\2f");
                filter = $"(&(name={identityEscaped}){filter})";
            }

            _results = LDAPHelper.QueryLDAP(searchBase, filter, properties, server, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADReplicationSubnet" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true),
                    //new StringArgument("SearchBase"),
                    new StringArgument("Identity"),
                    //new StringArgument("LDAPFilter"),
                    new StringArgument("Properties", "DistinguishedName,Location,Name,ObjectClass,ObjectGUID,Site")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets one or more Active Directory subnets."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Get all subnets", "Get-ADReplicationSubnet"),
                    new ExampleEntry("Get subnets with a specified name", "Get-ADReplicationSubnet -Identity \"10.0.10.0/24\""),
                    new ExampleEntry("Get the properties of a specified subnet", "Get-ADReplicationSubnet -Identity \"10.0.10.0/24\" -Properties *")
                };
            }
        }
    }
}
