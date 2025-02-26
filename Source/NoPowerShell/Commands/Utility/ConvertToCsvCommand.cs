using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class ConvertToCsvCommand : PSCommand
    {
        public ConvertToCsvCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string delimiter = _arguments.Get<StringArgument>("Delimiter").Value;

            if (pipeIn == null || pipeIn.Count == 0)
                return null;

            // Compile header
            string[] columns = new string[pipeIn[0].Count];
            int i = 0;
            foreach(KeyValuePair<string, string> col in pipeIn[0])
            {
                columns[i] = col.Key;
                i++;
            }

            Console.WriteLine("\"{0}\"", (string.Join($"\"{delimiter}\"", columns)));

            // Compile records
            foreach (ResultRecord result in pipeIn)
            {
                // Collect values
                string[] values = new string[result.Count];
                i = 0;
                foreach(KeyValuePair<string, string> kvp in result)
                {
                    // Escape quote by double quotes
                    values[i] = kvp.Value != null ? kvp.Value.Replace("\"", "\"\"") : "";
                    i++;
                }

                // Store values
                Console.WriteLine("\"{0}\"", (string.Join($"\"{delimiter}\"", values)));
            }

            return null;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "ConvertTo-Csv" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Delimiter", ","),
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Converts .NET objects into a series of character-separated value (CSV) strings."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Display process list as CSV", @"Get-Process | ConvertTo-Csv"),
                };
            }
        }
    }
}
