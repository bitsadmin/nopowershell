using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.IO;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class ExportCsvCommand : PSCommand
    {
        public ExportCsvCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string path = _arguments.Get<StringArgument>("Path").Value;
            string encodingstr = _arguments.Get<StringArgument>("Encoding").Value;

            if (pipeIn == null || pipeIn.Count == 0)
                return null;

            // Determine encoding
            Encoding encoding = Encoding.GetEncoding(encodingstr);

            // Initialize output
            string[] outlines = new string[pipeIn.Count + 1];
            int currentLine = 0;

            // Compile header
            string[] columns = new string[pipeIn[0].Count];
            int i = 0;
            foreach(KeyValuePair<string, string> col in pipeIn[0])
            {
                columns[i] = col.Key;
                i++;
            }
            outlines[currentLine] = string.Format("\"{0}\"", (string.Join("\",\"", columns)));
            currentLine++;

            // Compile records
            foreach (ResultRecord result in pipeIn)
            {
                // Collect values
                string[] values = new string[result.Count];
                i = 0;
                foreach(KeyValuePair<string, string> kvp in result)
                {
                    // Escape quote by double quotes
                    values[i] = kvp.Value.Replace("\"", "\"\"");
                    i++;
                }

                // Store values
                outlines[currentLine] = string.Format("\"{0}\"", (string.Join("\",\"", values)));
                currentLine++;
            }
            
            // Save to file with specified encoding
            File.WriteAllLines(path, outlines, encoding);

            return null;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Export-Csv", "epcsv" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("Encoding", "Unicode")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Converts objects into a series of comma-separated (CSV) strings and saves the strings in a CSV file."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Store list of commands as CSV", @"Get-Command | Export-Csv -Encoding ASCII -Path commands.csv"),
                };
            }
        }
    }
}
