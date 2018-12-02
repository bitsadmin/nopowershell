using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetSmbMapping : PSCommand
    {
        public GetSmbMapping(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            _results = WmiHelper.ExecuteWmiQuery(@"ROOT\Microsoft\Windows\SMB", "Select LocalPath,RemotePath From MSFT_SmbMapping");
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get {
                return new CaseInsensitiveList()
                {
                    "Get-NetSmbMapping",
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
                    new ExampleEntry("List network shares on the local machine that are exposed to the network", "Get-NetSmbMapping"),
                };
            }
        }
    }
}
