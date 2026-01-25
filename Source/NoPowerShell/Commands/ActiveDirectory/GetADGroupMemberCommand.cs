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
    public class GetADGroupMemberCommand : PSCommand
    {
        public GetADGroupMemberCommand(string[] userArguments) : base(userArguments)
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

            // Obtain distinguishedname for group
            CommandResult dn = LDAPHelper.QueryLDAP(
                string.Format("(&(objectCategory=group)(cn={0}))", identity),
                new List<string>(1) { "distinguishedName" }
            );

            // Return error if group is not found
            if (dn.Count == 0)
            {
                Console.WriteLine($"{Aliases[0]}: Cannot find an object with identity: '{identity}'.");
                return dn;
            }

            // Obtain group members
            string distinguishedName = dn[0]["distinguishedName"];
            _results = LDAPHelper.QueryLDAP(
                null,
                string.Format("(memberOf={0})", distinguishedName),
                new List<string>(1) { "distinguishedName", "name", "objectClass", "objectGUID", "SamAccountName", "SID" },
                server,
                username,
                password
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-ADGroupMember"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Server", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            new StringArgument("Identity")
        };

        public static new string Synopsis => "Gets the members of an Active Directory group.";

        public static new ExampleEntries Examples => new ExampleEntries()
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
