using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    /// <summary>
    /// Base class for all cmdlets
    /// </summary>
    public class PSCommand
    {
        protected ArgumentList _arguments;
        protected CommandResult _results;

        /// <summary>
        /// Construct a new PSCommand parsing the provided arguments using the provided list of arguments supported by this cmdlet
        /// </summary>
        /// <param name="userArguments">Arguments to be parsed</param>
        /// <param name="supportedArguments">Arguments supported by this cmdlet</param>
        public PSCommand(string[] userArguments, ArgumentList supportedArguments)
        {
            // To any command add support for specifying a remote computername including username and password
            // It is up to the command whether it makes use of these optional parameters
            supportedArguments.AddRange(new List<Argument>()
            {
                new StringArgument("ComputerName", ".", true),
                new StringArgument("Username", true),
                new StringArgument("Password", true)
            });

            _arguments = ParseArguments(userArguments, supportedArguments);
            _results = new CommandResult();
        }

        /// <summary>
        /// Parses arguments provided by user using the list of arguments supported by the cmdlet
        /// </summary>
        /// <param name="userArguments">Arguments to be parsed</param>
        /// <param name="supportedArguments">Arguments supported by this cmdlet</param>
        /// <returns>Parsed arguments</returns>
        protected ArgumentList ParseArguments(string[] userArguments, ArgumentList supportedArguments)
        {
            if (userArguments == null)
                return supportedArguments;

            // Iterate over user-provided arguments
            int i = 0;
            while (i < userArguments.Length)
            {
                string inputArg = userArguments[i];

                // Iterate over the arguments that are available for this cmdlet
                List<Argument> candidates = new List<Argument>(userArguments.Length);
                foreach (Argument destArg in supportedArguments)
                {
                    // Two options:
                    // 1) Provided argument matches expected argument
                    // 2) It could be an argument which doesn't need to be "cmd -Argname [value]". It can simply be "cmd [value]"
                    if (destArg.Name.Equals(inputArg.Substring(1, inputArg.Length - 1), StringComparison.InvariantCultureIgnoreCase))
                    {
                        candidates.Add(destArg);
                    }
                    else if (!inputArg.StartsWith("-") && !destArg.IsOptionalArgument)
                    {
                        destArg.DashArgumentNameSkipUsed = true;
                        candidates.Add(destArg);
                    }
                }

                if (candidates.Count == 0)
                {
                    throw new ArgumentException(string.Format("No parameter of {0} found matching \"{1}\".", this.ToString(), inputArg));
                }

                // Attempt to assign the variable to whatever argument has not been assigned yet
                bool assignedValue = false;
                foreach (Argument a in candidates)
                {
                    if (a.GetType() == typeof(BoolArgument))
                    {
                        ((BoolArgument)a).Value = true;
                        assignedValue = true;
                        break;
                    }
                    else if(a.GetType() == typeof(IntegerArgument))
                    {
                        ((IntegerArgument)a).Value = Convert.ToInt32(userArguments[++i]);
                        assignedValue = true;
                        break;
                    }
                    else if (a.GetType() == typeof(StringArgument))
                    {
                        StringArgument aCasted = (StringArgument)a;
                        if (aCasted.Value != null && !aCasted.IsDefaultValue)
                            continue;

                        // Optional StringArgument which requires a value
                        // For example Get-WmiObject -Namespace root\cimv2 -Query "Select CommandLine From Win32_Process"
                        if (!a.DashArgumentNameSkipUsed)
                            i++;

                        // Possibilities:
                        // - arg1,arg2
                        // - arg1, arg2
                        // Not supported yet: arg1 ,arg2
                        StringBuilder strbargs = new StringBuilder();
                        for (int j = i; j < userArguments.Length; j++)
                        {
                            string candidateArg = userArguments[j];

                            if (candidateArg.EndsWith(","))
                            {
                                strbargs.Append(userArguments[j]);
                                i++;
                            }
                            else
                                break;
                        }

                        bool onlyArgument = strbargs.Length == 0;
                        string strargs = strbargs.Append(userArguments[i]).ToString();

                        // Array where current component is last one
                        if (!onlyArgument)
                            i++;

                        aCasted.Value = strargs;
                        assignedValue = true;
                        break;
                    }
                }

                if (!assignedValue)
                    throw new Exception("Failed to assign value to parameter");

                i++;
            }

            return supportedArguments;
        }

        /// <summary>
        /// Implementation of the cmdlet
        /// </summary>
        /// <param name="pipeIn">Output from previous command in pipe</param>
        /// <returns></returns>
        public virtual CommandResult Execute(CommandResult pipeIn)
        {
            throw new InvalidOperationException("This function should be overridden");
        }

        /// <summary>
        /// Command + aliases of PSCommand. Is used for displaying help and to determine which command a user wants to execute.
        /// </summary>
        public virtual List<string> Aliases
        {
            get { throw new InvalidOperationException("This attribute should be overridden"); }
        }

        /// <summary>
        /// Name of cmdlet
        /// </summary>
        public string Command
        {
            // First command in list of Aliases should always be the full cmdlet
            get { return Aliases[0];  }
        }

        /// <summary>
        /// List of supported arguments. Order of the arguments will be reflected in the help (Get-Command).
        /// </summary>
        public virtual ArgumentList SupportedArguments
        {
            get { throw new InvalidOperationException("This attribute should be overridden"); }
        }

        public virtual string Synopsis
        {
            get { throw new InvalidOperationException("This attribute should be overridden"); }
        }

        public override string ToString()
        {
            PropertyInfo aliasesProperty = this.GetType().GetProperty("Aliases", BindingFlags.Static | BindingFlags.Public);
            CaseInsensitiveList aliases = (CaseInsensitiveList)aliasesProperty.GetValue(null, null);
            return aliases[0];
        }
    }
}