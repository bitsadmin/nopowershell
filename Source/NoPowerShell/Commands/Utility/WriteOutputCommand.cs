using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class WriteOutputCommand : PSCommand
    {
        public WriteOutputCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string inputObject = _arguments.Get<StringArgument>("InputObject").Value;

            // Push input string in pipe
            _results.Add(
                new ResultRecord()
                {
                    { string.Empty, inputObject }
                }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Write-Output", "echo", "write" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("InputObject", false)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Sends the specified objects to the next command in the pipeline. If the command is the last command in the pipeline, the objects are displayed in the console."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Echo string to the console",
                        new List<string>()
                        {
                            "Write-Output \"Hello World!\"",
                            "echo \"Hello World!\""
                        }
                    )
                };
            }
        }
    }
}
