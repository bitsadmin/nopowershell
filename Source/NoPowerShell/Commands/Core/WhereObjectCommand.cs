using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Core
{
    public class WhereObjectCommand : PSCommand
    {
        public WhereObjectCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            string property = _arguments.Get<StringArgument>("Property").Value;
            bool eq = _arguments.Get<BoolArgument>("EQ").Value;
            bool ne = _arguments.Get<BoolArgument>("NE").Value;
            bool like = _arguments.Get<BoolArgument>("Like").Value;
            string value = _arguments.Get<StringArgument>("Value").Value;

            if (pipeIn == null)
                return _results;

            // Iterate over output lines of previous command in pipe
            foreach (ResultRecord result in pipeIn)
            {
                // In case property does not exist, skip
                if (!result.ContainsKey(property))
                    continue;

                string tablevalue = result[property].ToLowerInvariant();
                string checkvalue = value.ToLowerInvariant();
                string cleancheckvalue = checkvalue.TrimStart('*').TrimEnd('*');
                bool foundValue = false;

                // Name -eq "value"
                if (eq)
                {
                    foundValue = (tablevalue == checkvalue);
                }
                // Name -ne "value"
                else if (ne)
                {
                    foundValue = (tablevalue != checkvalue);
                }
                // Name -like "value"
                else if (like)
                {
                    if (checkvalue.StartsWith("*"))
                    {
                        // - *value*
                        if (checkvalue.EndsWith("*"))
                            foundValue = tablevalue.Contains(cleancheckvalue);
                        // - *value
                        else
                            foundValue = tablevalue.EndsWith(cleancheckvalue);
                    }
                    else
                    {
                        // - value*
                        if (checkvalue.EndsWith("*"))
                            foundValue = tablevalue.StartsWith(cleancheckvalue);
                        // - value
                        else
                            foundValue = tablevalue == cleancheckvalue;
                    }
                }

                if (foundValue)
                    _results.Add(result);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Where-Object", "where", "?" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Property"),
                    new BoolArgument("EQ"),
                    new BoolArgument("NE"),
                    new BoolArgument("Like"),
                    new StringArgument("Value")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Selects objects from a collection based on their property values."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all processes containing PowerShell in the process name", "Get-Process | ? Name -Like *PowerShell*"),
                    new ExampleEntry("List all active local users", "Get-LocalUser | ? Disabled -EQ False")
                };
            }
        }
    }
}
