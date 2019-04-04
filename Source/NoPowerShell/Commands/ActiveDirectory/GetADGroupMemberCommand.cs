using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADGroupMemberCommand : PSCommand
    {
        public GetADGroupMemberCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string identity = _arguments.Get<StringArgument>("Identity").Value;

            // Obtain distinguishedname for group
            CommandResult dn = LDAPHelper.QueryLDAP(
                string.Format("(&(objectCategory=group)(cn={0}))", identity),
                new List<string>(1) { "distinguishedName" }
            );
            string distinguishedName = dn[0]["distinguishedName"];

            _results = LDAPHelper.QueryLDAP(
                string.Format("(memberOf={0})", distinguishedName),
                new List<string>(1) { "distinguishedName", "name", "objectClass", "objectGUID", "SamAccountName", "SID" }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADGroupMember" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Identity"),
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the members of an Active Directory group."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "List all members of the \"Domain Admins\" group",
                        new List<string>()
                        {
                            "Get-ADGroupMember -Identity \"Domain Admins\"",
                            "Get-ADGroupMember \"Domain Admins\""
                        }
                    )
                };
            }
        }
    }
}
