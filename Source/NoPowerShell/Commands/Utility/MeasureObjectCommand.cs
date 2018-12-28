using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class MeasureObjectCommand : PSCommand
    {
        public MeasureObjectCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            int count = -1;

            // Count lines if one block of output
            if(pipeIn.Count == 1 && pipeIn[0].Keys.Count == 1 && pipeIn[0].ContainsKey(string.Empty))
            {
                string blob = pipeIn[0][string.Empty];
                count = blob.Split('\n').Length;
            }
            // Count number of records
            else
            {
                count = pipeIn.Count;
            }

            _results.Add(
                new ResultRecord()
                {
                    { "Count", count.ToString() }
                }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Measure-Object", "measure" }; }
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
            get { return "Calculates the numeric properties of objects, and the characters, words, and lines in string objects, such as files of text."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Count number of results", "Get-Process | measure"),
                    new ExampleEntry("Count number of lines in file", "gc C:\\Windows\\WindowsUpdate.log | measure"),
                };
            }
        }
    }
}
