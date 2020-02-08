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
            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string searchBase = _arguments.Get<StringArgument>("SearchBase").Value;
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            List<string> properties = new List<string>(_arguments.Get<StringArgument>("Properties").Value.Split(','));

            // Input check
            SearchScope scope;
            if (string.IsNullOrEmpty(ldapFilter))
            {
                // Neither LDAPFilter nor Identity specified
                if (string.IsNullOrEmpty(identity))
                    throw new InvalidOperationException("Either LDAPFilter or Identity parameter is required");
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

            // Query
            _results = LDAPHelper.QueryLDAP(searchBase, scope, ldapFilter, properties, server, username, password);

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
                    new StringArgument("SearchBase"),
                    new StringArgument("Identity"),
                    new StringArgument("LDAPFilter"),
                    new StringArgument("Properties", "DistinguishedName,Name,ObjectClass,ObjectGUID", true)
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
                };
            }
        }
    }
}
