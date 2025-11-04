using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
                new StringArgument("ComputerName", "."),
                new StringArgument("Username", true),
                new StringArgument("Password", true),
                new BoolArgument("Verbose"),
                new BoolArgument("WhatIf")
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

                // Find candidate arguments
                List<Argument> candidates = FindCandidateArguments(userArguments, supportedArguments, inputArg, isArgFlag, cleanInputArg);

                // Handle no matching parameters
                if (candidates.Count == 0)
                {
                    throw new ParameterBindingException(this.ToString(), inputArg);
                }

                // Handle ambiguous parameters
                HandleAmbiguousParameters(candidates, cleanInputArg);

                // Handle multiple candidates by prioritizing mandatory ones
                if (candidates.Count > 1)
                {
                    candidates = PrioritizeMandatoryCandidates(candidates);
                }

                // Assign value to the argument
                i = AssignValueToArgument(userArguments, supportedArguments, i, candidates, inputArg);
                
                i++;
            }

            // Validate if all mandatory arguments have been assigned
            ValidateMandatoryArguments(supportedArguments);

            return supportedArguments;
        }

        /// <summary>
        /// Finds candidate arguments matching the user input
        /// </summary>
        private List<Argument> FindCandidateArguments(string[] userArguments, ArgumentList supportedArguments, string inputArg, bool isArgFlag, string cleanInputArg)
        {
            List<Argument> candidates = new List<Argument>(userArguments.Length);
            foreach (Argument destArg in supportedArguments)
            {
                // Skip arguments which have already been assigned
                if (destArg.IsSet)
                    continue;

                // Clone the argument based on its type
                Argument clonedArg = CloneArgument(destArg);
                if (clonedArg == null)
                    throw new Exception("Unexpected argument type");

                int compareLength = Math.Min(destArg.Name.Length, cleanInputArg.Length);
                
                if (isArgFlag)
                {
                    // Check for exact or partial match
                    bool isFound = false;
                    bool exactMatch = false;

                    // Full match: -ArgName
                    if (destArg.Name.Equals(cleanInputArg, StringComparison.InvariantCultureIgnoreCase))
                    {
                        isFound = true;
                        exactMatch = true;
                    }
                    // Partial match: -ArgN
                    else if (destArg.Name.Substring(0, compareLength).Equals(cleanInputArg, StringComparison.InvariantCultureIgnoreCase))
                    {
                        isFound = true;
                    }

                    // Add to candidates if matching argument is found
                    if (isFound)
                    {
                        candidates.Add(clonedArg);
                        if (exactMatch)
                            break;
                    }
                }
                // Handle positional arguments
                else if (destArg.GetType() != typeof(BoolArgument))
                {
                    if (clonedArg is StringArgument stringArg)
                    {
                        stringArg.DashArgumentNameSkipUsed = true;
                        candidates.Add(stringArg);
                    }
                    else if (clonedArg is IntegerArgument intArg)
                    {
                        intArg.DashArgumentNameSkipUsed = true;
                        candidates.Add(intArg);
                    }
                    else
                        throw new Exception("Unexpected positional argument type");
                }
            }

            return candidates;
        }

        /// <summary>
        /// Clones an argument based on its type
        /// </summary>
        private Argument CloneArgument(Argument destArg)
        {
            if (destArg.GetType() == typeof(StringArgument))
                return ((StringArgument)destArg).Clone();
            else if (destArg.GetType() == typeof(IntegerArgument))
                return ((IntegerArgument)destArg).Clone();
            else if (destArg.GetType() == typeof(BoolArgument))
                return ((BoolArgument)destArg).Clone();

            return null;
        }

        /// <summary>
        /// Handles potentially ambiguous parameters
        /// </summary>
        private void HandleAmbiguousParameters(List<Argument> candidates, string cleanInputArg)
        {
            if (candidates.Count <= 1)
                return;

            // Collect candidates that are not optional and do not have a dash argument name skip used
            List<Argument> duplicateCandidates = candidates.Where(c => !c.DashArgumentNameSkipUsed).ToList();

            // Handle ambiguous parameter name
            if (duplicateCandidates.Count > 1)
            {
                string[] paramNames = duplicateCandidates.Select(dc => dc.Name).ToArray();
                
                throw new ParameterBindingException(
                    this.ToString(),
                    string.Format(
                        "Parameter cannot be processed because the parameter name '{1}' is ambiguous. Possible matches include: -{2}.",
                        this,
                        cleanInputArg,
                        string.Join(" -", paramNames)
                    )
                );
            }
        }

        /// <summary>
        /// Prioritizes mandatory candidates over optional ones
        /// </summary>
        private List<Argument> PrioritizeMandatoryCandidates(List<Argument> candidates)
        {
            List<Argument> mandatoryCandidates = candidates.Where(c => !c.IsOptionalArgument).ToList();
            
            // If there are no mandatory candidates, use the first one
            if (mandatoryCandidates.Count == 0)
                return new List<Argument>() { candidates[0] };
            
            return mandatoryCandidates;
        }

        /// <summary>
        /// Assigns a value to the appropriate argument
        /// </summary>
        private int AssignValueToArgument(string[] userArguments, ArgumentList supportedArguments, int currentIndex, List<Argument> candidates, string inputArg)
        {
            string cleanInputArg = inputArg.TrimStart('-');
            bool assignedValue = false;

            foreach (Argument a in candidates)
            {
                if (a is BoolArgument boolArg)
                {
                    assignedValue = AssignBoolArgumentValue(supportedArguments, boolArg);
                    break;
                }
                else if (a is IntegerArgument intArg)
                {
                    currentIndex = AssignIntArgumentValue(userArguments, supportedArguments, currentIndex, intArg);
                    assignedValue = true;
                    break;
                }
                else if (a is StringArgument stringArg)
                {
                    if (stringArg.Value != null && !stringArg.IsDefaultValue)
                        continue;

                    currentIndex = AssignStringArgumentValue(userArguments, supportedArguments, currentIndex, stringArg);
                    assignedValue = true;
                    break;
                }
            }

            if (!assignedValue)
                throw new Exception(
                    string.Format(
                        @"{0}: Failed to assign value to parameter. This can be because of:
- A missing parameter name (e.g. 'MyValue' was used instead of '-Name MyValue')
- Duplicate arguments (e.g. '-Name MyValue -Name MyValue2')
- A missing pipe (' | ')",
                        this
                    )
                );

            return currentIndex;
        }

        /// <summary>
        /// Assigns a value to a boolean argument
        /// </summary>
        private bool AssignBoolArgumentValue(ArgumentList supportedArguments, BoolArgument boolArg)
        {
            boolArg.Value = true;

            // Update original ArgumentList with the value
            foreach (Argument originalArg in supportedArguments)
            {
                if (originalArg.Name.Equals(boolArg.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    ((BoolArgument)originalArg).Value = boolArg.Value;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Assigns a value to an integer argument
        /// </summary>
        private int AssignIntArgumentValue(string[] userArguments, ArgumentList supportedArguments, int currentIndex, IntegerArgument intArg)
        {
            // TODO: Currently simply overwrites the value if it is already set
            // Example: Get-ChildItem -Depth 1 -Depth 2
            intArg.Value = Convert.ToInt32(userArguments[++currentIndex]);

            // Update original ArgumentList with the value
            foreach (Argument originalArg in supportedArguments)
            {
                if (originalArg.Name.Equals(intArg.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    IntegerArgument originalArgCasted = (IntegerArgument)originalArg;
                    originalArgCasted.Value = intArg.Value;
                    originalArgCasted.DashArgumentNameSkipUsed = true;
                    break;
                }
            }

            return currentIndex;
        }

        /// <summary>
        /// Assigns a value to a string argument
        /// </summary>
        private int AssignStringArgumentValue(string[] userArguments, ArgumentList supportedArguments, int currentIndex, StringArgument stringArg)
        {
            // Positional StringArgument which requires a value
            if (!stringArg.DashArgumentNameSkipUsed)
                currentIndex++;

            // Handle comma-separated arguments
            StringBuilder strbargs = new StringBuilder();
            for (int j = currentIndex; j < userArguments.Length; j++)
            {
                string candidateArg = userArguments[j];

                if (candidateArg.EndsWith(","))
                {
                    strbargs.Append(userArguments[j]);
                    currentIndex++;
                }
                else
                    break;
            }

            bool onlyArgument = strbargs.Length == 0;
            string strargs = strbargs.Append(userArguments[currentIndex]).ToString();

            // Array where current component is last one
            if (!onlyArgument)
                currentIndex++;

            stringArg.Value = UnescapeString(strargs);

            // Update original ArgumentList with the value and set the DashArgumentNameSkipUsed flag
            foreach (Argument originalArg in supportedArguments)
            {
                if (originalArg.Name.Equals(stringArg.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    StringArgument originalArgCasted = (StringArgument)originalArg;
                    originalArgCasted.Value = stringArg.Value;
                    originalArgCasted.DashArgumentNameSkipUsed = true;
                    break;
                }
            }

            return currentIndex;
        }

        /// <summary>
        /// Validates that all mandatory arguments have been assigned a value
        /// </summary>
        private void ValidateMandatoryArguments(ArgumentList supportedArguments)
        {
            foreach (Argument arg in supportedArguments)
            {
                if (!arg.IsOptionalArgument && !arg.IsSet)
                {
                    throw new Exception(string.Format("{0}: Mandatory parameter '{1}' is missing.", this, arg.Name));
                }
            }
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
                        char next = input[i + 1];

                        switch (next)
                        {
                            case '`':
                                result.Append('`');
                                i += 2; // Skip both backticks
                                continue;
                            case 't':
                                result.Append('\t');
                                i += 2; // Skip the backtick and 't'
                                continue;
                            case 'n':
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
        public virtual CommandResult Execute(CommandResult pipeIn = null)
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
            get { return Aliases[0]; }
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