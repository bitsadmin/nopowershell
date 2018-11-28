using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetWhoamiCommand : PSCommand
    {
        public GetWhoamiCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            bool showAll = _arguments.Get<BoolArgument>("All").Value;

            string[] DomainUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\');

            _results.Add(
                new ResultRecord()
                {
                    { "Domain", DomainUser[0] },
                    { "User", DomainUser[1] }
                }
            );

            if(showAll)
            {
                // TODO
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Whoami", "whoami" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("All")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Show details about the current user."; }
        }
    }
}
