using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.LocalAccounts
{
    public class GetLocalUserCommand : PSCommand
    {
        public GetLocalUserCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string name = _arguments.Get<StringArgument>("Name").Value;
            string sid = _arguments.Get<StringArgument>("SID").Value;

            string query = "Select Name, Description, Disabled{0} From Win32_UserAccount Where LocalAccount='True'{1}";

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
            get { return new CaseInsensitiveList() { "Get-LocalUser", "glu" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name"),
                    new StringArgument("SID", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets local user accounts."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all local users", "Get-LocalUser"),
                    new ExampleEntry("List details of a specific user", "Get-LocalUser Administrator")
                };
            }
        }
    }
}
