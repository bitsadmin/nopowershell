using System;
using System.Collections.Generic;
using System.DirectoryServices;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADReplicationSubnetCommand : PSCommand
    {
        public GetADReplicationSubnetCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute(pipeIn);

            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            //string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            List<string> properties = new List<string>(_arguments.Get<StringArgument>("Properties").Value.Split(','));

            // Query
            string configurationNamingContext = null;
            using (DirectoryEntry rootDse = LDAPHelper.InitializeDirectoryEntry("RootDSE", null, server, username, password))
            {
                rootDse.RefreshCache(new string[] { "configurationNamingContext" });
                configurationNamingContext = (string)rootDse.Properties["configurationNamingContext"].Value;
            }

            if(string.IsNullOrEmpty(configurationNamingContext))
                throw new Exception("Could not determine the configurationNamingContext of the domain");

            string searchBase = $"CN=Subnets,CN=Sites,{configurationNamingContext}";
            string filter = "(objectClass=subnet)";
            if (!string.IsNullOrEmpty(identity))
            {
                string identityEscaped = identity.Replace("/", "\\2f");
                filter = $"(&(name={identityEscaped}){filter})";
            }

            _results = LDAPHelper.QueryLDAP(searchBase, filter, properties, server, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-ADReplicationSubnet"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Server", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            //new StringArgument("SearchBase"),
            new StringArgument("Identity", true),
            //new StringArgument("LDAPFilter"),
            new StringArgument("Properties", "DistinguishedName,Location,Name,ObjectClass,ObjectGUID,Site")
        };

        public static new string Synopsis => "Gets one or more Active Directory subnets.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Get all subnets", "Get-ADReplicationSubnet"),
            new ExampleEntry("Get subnets with a specified name", "Get-ADReplicationSubnet -Identity \"10.0.10.0/24\""),
            new ExampleEntry("Get the properties of a specified subnet", "Get-ADReplicationSubnet -Identity \"10.0.10.0/24\" -Properties *")
        };
    }
}
