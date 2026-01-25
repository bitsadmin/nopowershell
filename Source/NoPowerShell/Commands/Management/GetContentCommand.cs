using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.IO;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetContentCommand : PSCommand
    {
        public GetContentCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string path = _arguments.Get<StringArgument>("Path").Value;
            string txt = File.ReadAllText(path);

            // Create a single ResultRecord with an empty name to simply display raw output
            _results.Add(
                new ResultRecord() {
                    { string.Empty, txt }
                }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-Content",
            "gc",
            "cat",
            "type"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Path")
        };

        public static new string Synopsis => "Gets the contents of a file.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry
            (
                "View contents of a file",
                new List<string>()
                {
                    "Get-Content C:\\Windows\\WindowsUpdate.log",
                    "cat C:\\Windows\\WindowsUpdate.log"
                }
            )
        };
    }
}
