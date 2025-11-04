using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class SortObjectCommand : PSCommand
    {
        public SortObjectCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string property = _arguments.Get<StringArgument>("Property").Value;
            bool descending = _arguments.Get<BoolArgument>("Descending").Value;
            bool unique = _arguments.Get<BoolArgument>("Unique").Value;

            // Finished if input pipe is empty
            if (pipeIn == null || pipeIn.Count == 0)
                return pipeIn;

            // If no specific field specified, use first one
            if (string.IsNullOrEmpty(property))
            {
                foreach (string field in pipeIn[0].Keys)
                {
                    property = field;
                    break;
                }
            }

            // Validate if property exists
            if (!pipeIn[0].ContainsKey(property))
                throw new NoPowerShellException("Attribute \"{0}\" does not exist", property);

            // Perform sort
            if(descending)
                pipeIn.Sort((x, y) => y[property].CompareTo(x[property]));
            else
                pipeIn.Sort((x, y) => x[property].CompareTo(y[property]));

            // Perform unique if specified
            if(unique)
            {
                CommandResult pipeOut = new CommandResult();

                int r1i = 0;
                bool first = true;
                foreach (ResultRecord r1 in pipeIn)
                {
                    // First record is always unique
                    if(first)
                    {
                        first = false;
                        pipeOut.Add(r1);
                        r1i++;
                        continue;
                    }

                    int r2i = 0;
                    bool foundDuplicate = false;
                    foreach (ResultRecord r2 in pipeIn)
                    {
                        // Don't compare with yourself
                        if (r1i == r2i)
                        {
                            r2i++;
                            continue;
                        }

                        // Compare objects or property
                        if (
                            // Unique on full object
                            (string.IsNullOrEmpty(property) && r1.Equals(r2))
                            ||
                            // Unique on specific property
                            (!string.IsNullOrEmpty(property) && r1[property] == r2[property])
                        )
                        {
                            foundDuplicate = true;
                            break;
                        }

                        r2i++;
                    }

                    // Add if not found twice
                    if (!foundDuplicate)
                        pipeOut.Add(r1);

                    r1i++;
                }

                return pipeOut;
            }

            return pipeIn;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Sort-Object", "sort" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Property"),
                    new BoolArgument("Descending"),
                    new BoolArgument("Unique")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Sorts objects by property values."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Sort processes by name descending", "ps | sort -d name")
                };
            }
        }
    }
}
