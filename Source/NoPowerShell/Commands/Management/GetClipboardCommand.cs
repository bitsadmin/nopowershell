using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetClipboardCommand : PSCommand
    {
        public GetClipboardCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            //bool showHistory = _arguments.Get<BoolArgument>("History").Value;

            _results.Add(
                new ResultRecord()
                {
                    { "Data", Clipboard.GetText() }
                }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Clipboard", "gcb" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    //new BoolArgument("History")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the current Windows clipboard entry."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Show text contents of clipboard",
                        new List<string>()
                        {
                            "Get-Clipboard",
                            "gcb"
                        }
                    )
                };
            }
        }
    }
}
