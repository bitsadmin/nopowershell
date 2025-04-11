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
    public class SelectObjectCommand : PSCommand
    {
        public SelectObjectCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string[] attributes = _arguments.Get<StringArgument>("Property").Value.Split(',');
            int first = _arguments.Get<IntegerArgument>("First").Value;

            if (pipeIn == null)
                return null;

            bool wildcardSelect = attributes[0] == "*";
            bool firstSet = first > 0;

            // Simply return pipeIn if empty
            if (pipeIn.Count == 0)
                return pipeIn;

            // Ignore casing
            List<string> correctAttributes = new List<string>();
            foreach(string inputAttr in attributes)
            {
                bool found = false;

                // Locate case-insensitive match
                foreach (string key in pipeIn[0].Keys)
                {
                    if(inputAttr.Equals(key, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        correctAttributes.Add(key);
                        found = true;
                        break;
                    }
                }

                // Add original non-existent column if no matching column found
                if (!found)
                    correctAttributes.Add(inputAttr);
            }
            attributes = correctAttributes.ToArray();

            int counter = 0;
            foreach (ResultRecord result in pipeIn)
            {
                // Obey -First [int32] parameter and break if number is reached
                if (firstSet && counter == first)
                    break;

                // If all attributes need to be taken
                if (wildcardSelect)
                {
                    _results.Add(result);
                }
                // If specific attributes are selected
                else
                {
                    ResultRecord newResult = new ResultRecord();

                    foreach (string attr in attributes)
                    {
                        if (result.ContainsKey(attr))
                            newResult.Add(attr, result[attr]);
                        else
                            newResult.Add(attr, null);
                    }

                    _results.Add(newResult);
                }

                counter++;
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Select-Object", "select" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Property", "*"),
                    new IntegerArgument("First", 0)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Selects objects or object properties."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Show only the Name in a file listing", "ls C:\\ | select Name"),
                    new ExampleEntry("Show first 10 results of file listing", "ls C:\\Windows\\System32 -Include *.exe | select -First 10 Name,Length")
                };
            }
        }
    }
}
