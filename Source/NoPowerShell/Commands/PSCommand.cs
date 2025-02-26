using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
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
        // Arguments and pipe output
        protected ArgumentList _arguments;
        protected CommandResult _results;

        // Remote commands
        protected string computername;
        protected string username;
        protected string password;

        // Default parameters
        protected bool verbose = false;
        protected bool whatif = false;

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
                new StringArgument("Password", true),
                new BoolArgument("Verbose", false),
                new BoolArgument("WhatIf", false)
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
                bool isArgFlag = inputArg.StartsWith("-");
                string cleanInputArg = inputArg.TrimStart('-');

                // Iterate over the arguments that are available for this cmdlet
                List<Argument> candidates = new List<Argument>(userArguments.Length);
                foreach (Argument destArg in supportedArguments)
                {
                    // Two options:
                    // 1) Provided argument matches expected argument
                    //    a) It is an exact match
                    //    b) It is a partial match
                    // 2) It could be an argument which doesn't need to be "cmd -Argname [value]". It can simply be "cmd [value]"
                    int compareLength = Math.Min(destArg.Name.Length, cleanInputArg.Length);
                    if(isArgFlag && destArg.Name.Equals(cleanInputArg, StringComparison.InvariantCultureIgnoreCase))
                    {
                        candidates = new List<Argument>() { destArg };
                        break;
                    }
                    else if (isArgFlag && destArg.Name.Substring(0, compareLength).Equals(cleanInputArg, StringComparison.InvariantCultureIgnoreCase))
                    {
                        candidates.Add(destArg);
                    }
                    else if (!isArgFlag && !destArg.IsOptionalArgument)
                    {
                        destArg.DashArgumentNameSkipUsed = true;
                        candidates.Add(destArg);
                    }
                }

                if (candidates.Count == 0)
                {
                    throw new ParameterBindingException(this.ToString(), inputArg);
                }
                else if (candidates.Count > 1)
                {
                    List<Argument> duplicateCandidates = new List<Argument>(candidates.Count);
                    foreach (Argument c in candidates)
                    {
                        if (!c.DashArgumentNameSkipUsed)
                            duplicateCandidates.Add(c);
                    }

                    // Ambiguous parameter name is provided
                    if (duplicateCandidates.Count > 1)
                    {
                        string[] paramNames = new string[duplicateCandidates.Count];
                        int j = 0;
                        foreach (Argument dc in duplicateCandidates)
                            paramNames[j++] = dc.Name;

                        throw new ParameterBindingException(
                            this.ToString(),
                            string.Format(
                                "Parameter cannot be processed because the parameter name '{0}' is ambiguous. Possible matches include: -{1}.",
                                cleanInputArg,
                                string.Join(" -", paramNames)
                            )
                        );
                    }
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

                        aCasted.Value = UnescapeString(strargs);
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
        /// Processes the input string by replacing two consecutive backticks with one backtick,
        /// replacing a backtick followed by 't' with a tab character, and replacing a backtick
        /// followed by 'n' with a newline character.
        /// </summary>
        /// <param name="input">The input string to process.</param>
        /// <returns>The processed string.</returns>
        public static string UnescapeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            StringBuilder result = new StringBuilder();
            int length = input.Length;
            int i = 0;

            while (i < length)
            {
                if (input[i] == '`')
                {
                    // Check if there's at least one more character
                    if (i + 1 < length)
                    {
                        if (input[i + 1] == '`')
                        {
                            // Replace `` with `
                            result.Append('`');
                            i += 2; // Skip both backticks
                            continue;
                        }
                        else if (input[i + 1] == 't')
                        {
                            // Replace `t with a tab character
                            result.Append('\t');
                            i += 2; // Skip the backtick and 't'
                            continue;
                        }
                        else if(input[i + 1] == 'n')
                        {
                            // Replace `n with a newline character
                            result.Append('\n');
                            i += 2; // Skip the backtick and 'n'
                            continue;
                        }
                    }

                    // If it's a single backtick without a special pattern, append as is
                    result.Append(input[i]);
                    i++;
                }
                else
                {
                    // Append regular characters
                    result.Append(input[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Implementation of the cmdlet.
        /// When calling the base class, it obtains the values from the ComputerName, Username and Password parameters and populates the corresponding variables.
        /// </summary>
        /// <param name="pipeIn">Output from previous command in pipe</param>
        /// <returns></returns>
        public virtual CommandResult Execute(CommandResult pipeIn=null)
        {
            // Remote parameters
            computername = _arguments.Get<StringArgument>("ComputerName").Value;
            username = _arguments.Get<StringArgument>("Username").Value;
            password = _arguments.Get<StringArgument>("Password").Value;

            // Common parameters
            verbose = _arguments.Get<BoolArgument>("Verbose").Value;
            whatif = _arguments.Get<BoolArgument>("WhatIf").Value;

            return pipeIn;
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

        public virtual ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries();
            }
        }

        public override string ToString()
        {
            PropertyInfo aliasesProperty = this.GetType().GetProperty("Aliases", BindingFlags.Static | BindingFlags.Public);
            CaseInsensitiveList aliases = (CaseInsensitiveList)aliasesProperty.GetValue(null, null);
            return aliases[0];
        }
    }
}