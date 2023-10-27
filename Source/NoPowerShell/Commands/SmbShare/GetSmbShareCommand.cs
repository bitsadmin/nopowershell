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
        public GetSmbShareCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            _results = WmiHelper.ExecuteWmiQuery("Select * From Win32_Share", computername, username, password);
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get {
                return new CaseInsensitiveList()
                {
                    "Get-SmbShare",
                    "netshare" // Not official
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Retrieves the SMB shares on the computer."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List SMB shares on the computer", "Get-SmbShare"),
                };
            }
        }
    }
}
