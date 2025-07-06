using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Core
{
    public class GetCommandCommand : PSCommand
    {
        public GetCommandCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            bool included = _arguments.Get<BoolArgument>("_Included").Value;
            bool cheatsheet = _arguments.Get<BoolArgument>("_Cheatsheet").Value;
            bool tsv = _arguments.Get<BoolArgument>("_Tsv").Value;
            string moduleFilter = _arguments.Get<StringArgument>("Module").Value;

            // Get all commands
            Dictionary<Type, CaseInsensitiveList> commandTypes = ReflectionHelper.GetCommands();

            // Intialize hashtable for cheatsheet
            Hashtable exampletable = new Hashtable();

            // Iterate over all available cmdlets
            foreach (KeyValuePair<Type, CaseInsensitiveList> commandType in commandTypes)
            {
                // Hide TemplateCommand from list of commands
                // It is available to experiment with though
                if (commandType.Key == typeof(TemplateCommand))
                    continue;

                // Aliases
                CaseInsensitiveList aliases = commandType.Value;

                // Command name
                string commandName = aliases[0];

                // Arguments
                ArgumentList arguments = null;
                PropertyInfo argumentsProperty = commandType.Key.GetProperty("SupportedArguments", BindingFlags.Static | BindingFlags.Public);
                if (argumentsProperty != null)
                    arguments = (ArgumentList)argumentsProperty.GetValue(null, null);
                else
                    arguments = new ArgumentList();

                // Hide internal parameters
                ArgumentList newarguments = new ArgumentList();
                foreach (Argument arg in arguments)
                {
                    if (!arg.Name.StartsWith("_"))
                        newarguments.Add(arg);
                }
                arguments = newarguments;

                // Synopsis
                string strSynopsis = null;
                PropertyInfo synopsisProperty = commandType.Key.GetProperty("Synopsis", BindingFlags.Static | BindingFlags.Public);
                if (synopsisProperty != null)
                    strSynopsis = (string)synopsisProperty.GetValue(null, null);

                // Arguments
                string strArguments = GetArguments(arguments);

                // Aliases
                string strAliases = string.Join(", ", aliases.GetRange(1, aliases.Count - 1).ToArray());

                // Module
                string module = commandType.Key.Namespace.Replace("NoPowerShell.Commands.", string.Empty);
                if (!string.IsNullOrEmpty(moduleFilter) && moduleFilter.ToLowerInvariant() != module.ToLowerInvariant())
                    continue;

                // Store examples to generate the cheatsheet
                if (cheatsheet)
                {
                    PropertyInfo examplesProperty = commandType.Key.GetProperty("Examples", BindingFlags.Static | BindingFlags.Public);
                    ExampleEntries examples = (examplesProperty != null) ? (ExampleEntries)examplesProperty.GetValue(null, null) : null;
                    exampletable[commandName] = examples;
                }

                _results.Add(
                    new ResultRecord()
                    {
                        { "Command", string.Format("{0} {1}", commandName, strArguments) },
                        { "Aliases", strAliases },
                        { "Synopsis", strSynopsis },
                        { "Module", module }
                    }
                );
            }

            // Organize by module
            _results.Sort(delegate (ResultRecord a, ResultRecord b)
            {
                return a["Module"].CompareTo(b["Module"]);
            });

            // Generate markdown of all available commands
            // Make sure to run this having the project compiled as .NET 4.5 to include all commands
            if (included)
            {
                Console.WriteLine("| Cmdlet | Module | Notes |\r\n| - | - | - |");

                foreach (ResultRecord r in _results)
                {
                    Console.WriteLine("| {0} | {1} | |", r["Command"].Split(new char[] { ' ' })[0], r["Module"]);
                }

                Console.WriteLine("\r\n\r\nTotal: {0}", _results.Count);

                return null;
            }

            // Generate cheatsheet markdown
            if (cheatsheet)
            {
                if (tsv)
                    Console.WriteLine("Action\tCommand");
                else
                    Console.WriteLine("| Action | Command |\r\n| - | - |");

                foreach (ResultRecord r in _results)
                {
                    string cmdlet = r["Command"].Split(new char[] { ' ' })[0];
                    ExampleEntries examples = (ExampleEntries)exampletable[cmdlet];

                    foreach (ExampleEntry example in examples)
                    {
                        List<string> examplestrings;
                        if (tsv)
                        {
                            examplestrings = example.Examples;
                        }
                        else
                        {
                            examplestrings = new List<string>(example.Examples.Count);
                            foreach (string ex in example.Examples)
                                examplestrings.Add(ex.Replace("|", "\\|"));
                        }

                        int i = 0;
                        foreach (string ex in examplestrings)
                        {
                            string desc = string.Empty;
                            if (i == 0)
                                desc = example.Description;
                            else
                                desc = string.Format("{0} - Alternative", example.Description);

                            if(tsv)
                                Console.WriteLine("{0}\t{1}", desc, ex);
                            else
                                Console.WriteLine("| {0} | `{1}` |", desc, ex);

                            i++;
                        }
                    }
                }

                return null;
            }

            // Remove Module attribute
            foreach (ResultRecord r in _results)
            {
                r.Remove("Module");
            }

            return _results;
        }

        public static string GetArguments(ArgumentList arguments)
        {
            string[] strArgs = new string[arguments.Count];
            int i = 0;
            foreach (Argument arg in arguments)
            {
                // Bool arguments don't require a value, they are simply a flag
                if (arg.GetType() == typeof(BoolArgument))
                    strArgs[i] = string.Format("[-{0}]", arg.Name);
                // String arguments can both be mandatory and optional
                else if (arg.GetType() == typeof(StringArgument))
                {
                    if (arg.IsOptionalArgument)
                        strArgs[i] = string.Format("[-{0} [Value]]", arg.Name);
                    else
                        strArgs[i] = string.Format("-{0} [Value]", arg.Name);
                }
                else if (arg.GetType() == typeof(IntegerArgument))
                    strArgs[i] = string.Format("[-{0} [Int32]]", arg.Name);

                i++;
            }

            return string.Join(" ", strArgs);
        }

        public static new CaseInsensitiveList Aliases
        {
            get
            {
                return new CaseInsensitiveList() { "Get-Command" };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument ("_Included"),
                    new BoolArgument ("_Cheatsheet"),
                    new BoolArgument ("_Tsv"),
                    new StringArgument ("Module", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Shows all available commands."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all commands supported by NoPowerShell", "Get-Command"),
                    new ExampleEntry("List commands of a certain module", "Get-Command -Module ActiveDirectory")
                };
            }
        }
    }
}
