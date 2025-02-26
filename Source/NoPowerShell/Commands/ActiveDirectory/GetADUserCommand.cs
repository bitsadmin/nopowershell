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
    public class GetADUserCommand : PSCommand
    {
        public GetADUserCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string searchBase = _arguments.Get<StringArgument>("SearchBase").Value;
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            string filter = _arguments.Get<StringArgument>("Filter").Value;
            CaseInsensitiveList properties = new CaseInsensitiveList(_arguments.Get<StringArgument>("Properties").Value.Split(','));

            // Determine filters
            bool filledIdentity = !string.IsNullOrEmpty(identity);
            bool filledLdapFilter = !string.IsNullOrEmpty(ldapFilter);
            bool filledFilter = !string.IsNullOrEmpty(filter);

            // Input checks
            if (filledIdentity && filledLdapFilter)
                throw new NoPowerShellException("Specify either Identity or LDAPFilter, not both");
            if (!filledIdentity && !filledLdapFilter && !filledFilter)
                throw new NoPowerShellException("Specify either Identity, Filter or LDAPFilter");

            // Build filter
            string filterBase = "(&(objectCategory=user){0})";
            string queryFilter = string.Empty;

            // -Identity Administrator
            if (filledIdentity)
                queryFilter = string.Format(filterBase, string.Format("(sAMAccountName={0})", identity));

            // -LDAPFilter "(adminCount=1)"
            else if (filledLdapFilter)
            {
                queryFilter = string.Format(filterBase, ldapFilter);
            }

            // -Filter *
            else if (filledFilter)
            {
                // TODO: allow more types of filters
                if (filter != "*")
                    throw new NoPowerShellException("Currently only * filter is supported");

                queryFilter = string.Format(filterBase, string.Empty);
            }

            // Obtain search base if not specified
            if (string.IsNullOrWhiteSpace(searchBase))
                searchBase = LDAPHelper.GetDistinguishedName(server, username, password);

            // Query
            _results = LDAPHelper.QueryLDAP(searchBase, queryFilter, properties, server, username, password);

            // Translate UserAccountControl AD field into whether the account is enabled
            if (_results.Count > 0 && _results[0].ContainsKey("useraccountcontrol"))
            {
                foreach (ResultRecord r in _results)
                {
                    string uac = r["useraccountcontrol"];
                    bool active = LDAPHelper.IsActive(uac);
                    r["Enabled"] = active.ToString();
                    //r.Remove("useraccountcontrol");
                }
            }

            // Display error message if no results and identity is specified
            if (_results.Count == 0 && !string.IsNullOrEmpty(identity))
                Console.WriteLine($"{Aliases[0]}: Cannot find an object with identity: '{identity}' under '{searchBase}'.");

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADUser" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true),
                    new StringArgument("SearchBase", true),
                    new StringArgument("Identity"),
                    new StringArgument("Filter", true),
                    new StringArgument("LDAPFilter", true),
                    new StringArgument("Properties", "DistinguishedName,userAccountControl,GivenName,Name,ObjectClass,ObjectGUID,SamAccountName,ObjectSID,Surname,UserPrincipalName", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets one or more Active Directory users."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all properties of the Administrator domain user", "Get-ADUser -Identity Administrator -Properties *"),
                    new ExampleEntry("List all Administrative users in domain", "Get-ADUser -LDAPFilter \"(admincount=1)\""),
                    new ExampleEntry("List all users in domain", "Get-ADUser -Filter *"),
                    new ExampleEntry("List specific attributes of user", "Get-ADUser Administrator -Properties SamAccountName,ObjectSID"),
                    new ExampleEntry("List all users in a specific OU", "Get-ADUser -SearchBase \"CN=Users,DC=MyDomain,DC=local\" -Filter *")
                };
            }
        }
    }
}
