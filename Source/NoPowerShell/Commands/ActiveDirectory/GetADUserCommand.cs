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
            string identity = _arguments.Get<StringArgument>("Identity").Value;
            string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            string filter = _arguments.Get<StringArgument>("Filter").Value;
            string properties = _arguments.Get<StringArgument>("Properties").Value;

            // Determine filters
            bool filledIdentity = !string.IsNullOrEmpty(identity);
            bool filledLdapFilter = !string.IsNullOrEmpty(ldapFilter);
            bool filledFilter = !string.IsNullOrEmpty(filter);

            // Input checks
            if (filledIdentity && filledLdapFilter)
                throw new InvalidOperationException("Specify either Identity or LDAPFilter, not both");
            if (!filledIdentity && !filledLdapFilter && !filledFilter)
                throw new InvalidOperationException("Specify either Identity, Filter or LDAPFilter");

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
                    throw new InvalidOperationException("Currently only * filter is supported");

                queryFilter = string.Format(filterBase, string.Empty);
            }

            // Query
            _results = LDAPHelper.QueryLDAP(queryFilter, new List<string>(properties.Split(',')));

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
                };
            }
        }
    }
}
