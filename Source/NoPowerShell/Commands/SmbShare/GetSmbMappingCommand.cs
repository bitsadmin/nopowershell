using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.SmbShare
{
    public class GetSmbMapping : PSCommand
    {
        public GetSmbMapping(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute(pipeIn);

            // Obtain Username/Password parameters
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            _results = WmiHelper.ExecuteWmiQuery(@"ROOT\Microsoft\Windows\SMB", "Select LocalPath,RemotePath From MSFT_SmbMapping", computername, username, password);
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get {
                return new CaseInsensitiveList()
                {
                    "Get-SmbMapping",
                    "netuse" // Not official
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("ComputerName", true),
                    new StringArgument("Username", true),
                    new StringArgument("Password", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Retrieves the SMB client directory mappings created for a server."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "List mapped network drives",
                        new List<string>()
                        {
                            "Get-SmbMapping",
                            "netuse"
                        }
                    )
                };
            }
        }
    }
}
