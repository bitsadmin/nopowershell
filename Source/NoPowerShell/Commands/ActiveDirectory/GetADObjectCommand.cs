using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.DirectoryServices;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADObjectCommand : PSCommand
    {
        public GetADObjectCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain Username/Password parameters
            base.Execute(pipeIn);

            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string searchBase = _arguments.Get<StringArgument>("SearchBase").Value;
            string searchScopeString = _arguments.Get<StringArgument>("SearchScope").Value;
            int resultSetSize = _arguments.Get<IntegerArgument>("ResultSetSize").Value;
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            List<string> properties = new List<string>(_arguments.Get<StringArgument>("Properties").Value.Split(','));

            // Determine filters
            bool filledIdentity = !string.IsNullOrEmpty(identity);
            bool filledLdapFilter = !string.IsNullOrEmpty(ldapFilter);

            // Input checks
            if (filledIdentity && filledLdapFilter)
                throw new NoPowerShellException("Specify either Identity or LDAPFilter, not both");

            SearchScope scope;
            if (string.IsNullOrEmpty(ldapFilter))
            {
                // Neither LDAPFilter nor Identity specified
                if (string.IsNullOrEmpty(identity))
                    throw new NoPowerShellException("Either LDAPFilter or Identity parameter is required");
                // Identity is specified, so search on the base
                else
                {
                    searchBase = identity;
                    scope = SearchScope.Base;
                }
            }
            // LDAPFilter is specified
            else
                scope = SearchScope.Subtree;

            // Obtain SearchScope
            switch(searchScopeString.ToLowerInvariant())
            {
                case "base":
                    scope = SearchScope.Base;
                    break;
                case "onelevel":
                    scope = SearchScope.OneLevel;
                    break;
                case "subtree":
                    scope = SearchScope.Subtree;
                    break;
                case "":
                    break;
                default:
                    throw new NoPowerShellException("Unknown SearchScope value: {0}", searchScopeString);
            }

            // Obtain search base if not specified
            if (string.IsNullOrWhiteSpace(searchBase))
                searchBase = LDAPHelper.GetDistinguishedName(server, username, password);

            // Query
            _results = LDAPHelper.QueryLDAP(searchBase, scope, ldapFilter, properties, resultSetSize, server, username, password);

            // Display error message if no results and identity is specified
            if (_results.Count == 0 && !string.IsNullOrEmpty(identity))
                Console.WriteLine($"{Aliases[0]}: Cannot find an object with identity: '{identity}'.");

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADObject" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true),
                    new StringArgument("SearchBase", true),
                    new StringArgument("SearchScope", string.Empty),
                    new IntegerArgument("ResultSetSize", 0),
                    new StringArgument("Identity", true),
                    new StringArgument("LDAPFilter", true),
                    new StringArgument("Properties", "DistinguishedName,Name,ObjectClass,ObjectGUID")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets one or more Active Directory objects."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Get the sites from the configuration naming context", "Get-ADObject -LDAPFilter \"(objectClass=site)\" -SearchBase \"CN=Configuration,DC=MyDomain,DC=local\" -Properties whenCreated,cn"),
                    new ExampleEntry("Get specific object", "Get-ADObject -Identity \"CN=Directory Service,CN=Windows NT,CN=Services,CN=Configuration,DC=MyDomain,DC=local\" -Properties *"),
                    new ExampleEntry("List all global groups", "Get-ADObject –LDAPFilter \"(GroupType:1.2.840.113556.1.4.803:=2)\" –SearchBase \"DC=MyDomain,DC=local\""),
                    new ExampleEntry("List only users that are directly in the OU (not in sub-OUs)", "Get-ADObject -SearchBase \"CN=Users,DC=MyDomain,DC=local\" -LDAPFilter \"(objectClass=user)\" -SearchScope OneLevel"),
                    new ExampleEntry("Obtain distinguishedname of domain", "Get-ADObject -LDAPFilter \"(objectClass=*)\" -SearchScope Base -Server mydomain.com")
                };
            }
        }
    }
}
