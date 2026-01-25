using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.SmbShare
{
    public class GetSmbShareCommand : PSCommand
    {
        public GetSmbShareCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute(pipeIn);

            // Obtain Username/Password parameters
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            _results = WmiHelper.ExecuteWmiQuery("Select * From Win32_Share", computername, username, password);
            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-SmbShare",
            "netshare" // Not official
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("ComputerName", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true)
        };

        public static new string Synopsis => "Retrieves the SMB shares on the computer.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("List SMB shares on the computer", "Get-SmbShare"),
        };
    }
}
