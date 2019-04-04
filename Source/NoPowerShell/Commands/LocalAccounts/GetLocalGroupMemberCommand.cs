using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.LocalAccounts
{
    public class GetLocalGroupMemberCommand : PSCommand
    {
        public GetLocalGroupMemberCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Parameters
            string group = _arguments.Get<StringArgument>("Group").Value;
            string name = _arguments.Get<StringArgument>("Name").Value;

            string hostname = System.Environment.MachineName;
            string query = "Associators Of {{Win32_Group.Domain='{0}',Name='{1}'}} Where ResultClass = Win32_UserAccount";

            if (!string.IsNullOrEmpty(group))
                query = string.Format(query, hostname, group);
            else if (!string.IsNullOrEmpty(name))
                query = string.Format(query, hostname, name);
            else
                throw new InvalidOperationException("-Group or -Name parameter needs to be provided");

            _results = WmiHelper.ExecuteWmiQuery(query, computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-LocalGroupMember", "glgm" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Group"),
                    new StringArgument("Name", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets members from a local group."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all active members of the Administrators group", "Get-LocalGroupMember -Group Administrators | ? Disabled -eq False")
                };
            }
        }
    }
}
