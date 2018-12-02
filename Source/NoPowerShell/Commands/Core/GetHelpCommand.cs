using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetHelpCommand : PSCommand
    {
        public GetHelpCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string name = _arguments.Get<StringArgument>("Name").Value;

            // Show help for Get-Help if no name parameter is provided
            Type command = typeof(GetHelpCommand);

            // Determine command
            if (!string.IsNullOrEmpty(name))
            {
                Dictionary<Type, CaseInsensitiveList> commandTypes = ReflectionHelper.GetCommands();
                bool found = false;
                foreach (KeyValuePair<Type, CaseInsensitiveList> type in commandTypes)
                {
                    if (type.Value.Contains(name))
                    {
                        command = type.Key;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new InvalidOperationException(string.Format("Command {0} not found", name));
            }

            // Collect information
            // Aliases
            PropertyInfo aliasesProperty = command.GetProperty("Aliases", BindingFlags.Static | BindingFlags.Public);
            CaseInsensitiveList aliases = (aliasesProperty != null) ? (CaseInsensitiveList)aliasesProperty.GetValue(null, null) : null;

            // SupportedArguments
            PropertyInfo argumentsProperty = command.GetProperty("SupportedArguments", BindingFlags.Static | BindingFlags.Public);
            ArgumentList supportedArguments = (argumentsProperty != null) ? (ArgumentList)argumentsProperty.GetValue(null, null) : null;

            // Synopsis
            PropertyInfo synopsisProperty = command.GetProperty("Synopsis", BindingFlags.Static | BindingFlags.Public);
            string synopsis = (synopsisProperty != null) ? (string)synopsisProperty.GetValue(null, null) : null;

            // Examples
            PropertyInfo examplesProperty = command.GetProperty("Examples", BindingFlags.Static | BindingFlags.Public);
            ExampleEntries examples = (examplesProperty != null) ? (ExampleEntries)examplesProperty.GetValue(null, null) : null;

            // Compile man page
            string format =
@"NAME
    {0}


ALIASES
    {1}


SYNOPSIS
    {2}


SYNTAX
    {3}


EXAMPLES
{4}
";

            string commandName = (aliases != null) ? aliases[0] : null;
            string strAliases = string.Join(", ", aliases.GetRange(1, aliases.Count - 1).ToArray());
            string arguments = string.Format("{0} {1}", aliases[0], GetCommandCommand.GetArguments(supportedArguments));

            StringBuilder strExamples = new StringBuilder();
            if (examples != null)
            {
                int exampleId = 1;
                foreach (ExampleEntry helpEntry in examples)
                {
                    strExamples.AppendFormat("    --------------------- EXAMPLE {0} ---------------------\r\n\r\n", exampleId);
                    strExamples.AppendFormat("    Synopsis: {0}\r\n\r\n", helpEntry.Description);
                    foreach (string ex in helpEntry.Examples)
                    {
                        strExamples.AppendFormat("    C:\\> {0}\r\n\r\n", ex);
                    }
                    strExamples.AppendLine();
                    exampleId++;
                }
            }

            _results.Add(
                new ResultRecord()
                {
                    { string.Empty, string.Format(format, commandName, strAliases, synopsis, arguments, strExamples.ToString()) }
                }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Help", "man" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Displays information about NoPowerShell commands."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Get help for a command",
                        new List<string>()
                        {
                            "Get-Help -Name Get-Process",
                            "man ps"
                        }
                    )
                };
            }
        }
    }
}
