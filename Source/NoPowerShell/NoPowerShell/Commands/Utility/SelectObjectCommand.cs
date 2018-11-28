using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class SelectObjectCommand : PSCommand
    {
        public SelectObjectCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string[] attributes = _arguments.Get<StringArgument>("Property").Value.Split(',');

            foreach (ResultRecord result in pipeIn)
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
                    new StringArgument("Property", null)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Selects objects or object properties."; }
        }
    }
}
