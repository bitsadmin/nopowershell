using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.LocalAccounts
{
    public class GetLocalGroupCommand : PSCommand
    {
        public GetLocalGroupCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain Username/Password parameters
            base.Execute(pipeIn);

            string name = _arguments.Get<StringArgument>("Name").Value;
            string sid = _arguments.Get<StringArgument>("SID").Value;

            string query = "Select Name, Description{0} From Win32_Group Where LocalAccount='True'{1}";

            if (!string.IsNullOrEmpty(name))
                query = string.Format(query, ", SID", string.Format(" And Name='{0}'", name));
            else if (!string.IsNullOrEmpty(sid))
                query = string.Format(query, ", SID", string.Format(" And SID='{0}'", sid));
            else
                query = string.Format(query, string.Empty, string.Empty);

            _results = WmiHelper.ExecuteWmiQuery(query, computername, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-LocalGroup", "glg" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name", true),
                    new StringArgument("SID", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the local security groups."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all local groups", "Get-LocalGroup"),
                    new ExampleEntry("List details of a specific group", "Get-LocalGroup Administrators"),
                    new ExampleEntry
                    (
                        "List members of Administrators group on a remote computer using WMI",
                        new List<string>()
                        {
                            "Get-LocalGroup -ComputerName Myserver -Username MyUser -Password MyPassword -Name Administrators",
                            "Get-LocalGroup -ComputerName Myserver -Name Administrators"
                        }
                    )
                };
            }
        }
    }
}
