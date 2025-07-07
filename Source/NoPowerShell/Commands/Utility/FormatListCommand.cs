using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class FormatListCommand : PSCommand
    {
        public FormatListCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string[] properties = _arguments.Get<StringArgument>("Property").Value.Split(',');

            _results.Output = CommandResult.OutputType.List;

            if (pipeIn == null)
                return null;

            foreach (ResultRecord result in pipeIn)
            {
                // Show all columns
                if (properties[0] == string.Empty)
                    _results.Add(result);
                
                // Show only specific columns
                else
                {
                    ResultRecord newResult = new ResultRecord();

                    foreach (string attr in properties)
                    {
                        if (result.ContainsKey(attr))
                            newResult.Add(attr, result[attr]);
                        else
                            newResult.Add(attr, null);
                    }

                    _results.Add(newResult);
                }
            }

            string output = ResultPrinter.OutputResults(_results);

            // Store in output key
            return new CommandResult(1) {
                new ResultRecord(1)
                {
                    { "Output", output }
                }
            };
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Format-List", "fl" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Property", string.Empty)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Formats the output as a list of properties in which each property appears on a new line."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Format output as a list",
                        new List<string>()
                        {
                            "Get-LocalUser | Format-List",
                            "Get-LocalUser | fl"
                        }
                    ),
                    new ExampleEntry("Format output as a list showing only specific attributes", "Get-LocalUser | fl Name,Description"),
                };
            }
        }
    }
}
