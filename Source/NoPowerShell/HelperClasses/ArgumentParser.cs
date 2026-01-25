using NoPowerShell.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public static class ArgumentParser
    {
        public static ArgumentList ParseArguments(string[] userArguments, ArgumentList supportedArguments, string commandName)
        {
            if (supportedArguments == null)
                throw new ArgumentNullException(nameof(supportedArguments));

            if (userArguments is null)
                return supportedArguments;

            string effectiveCommandName = string.IsNullOrWhiteSpace(commandName) ? "Command" : commandName;

            // Iterate over user-provided arguments
            int i = 0;
            while (i < userArguments.Length)
            {
                string inputArg = userArguments[i];
                bool isArgFlag = inputArg.StartsWith("-");
                string cleanInputArg = inputArg.TrimStart('-');

                // Find candidate arguments
                List<Argument> candidates = FindCandidateArguments(userArguments, supportedArguments, isArgFlag, cleanInputArg);

                // Handle no matching parameters
                if (candidates.Count == 0)
                {
                    throw new ParameterBindingException(effectiveCommandName, inputArg);
                }

                // Handle ambiguous parameters
                HandleAmbiguousParameters(effectiveCommandName, candidates, cleanInputArg);

                // Handle multiple candidates by prioritizing mandatory ones
                if (candidates.Count > 1)
                {
                    candidates = PrioritizeMandatoryCandidates(candidates);
                }

                // Assign value to the argument
                i = AssignValueToArgument(effectiveCommandName, userArguments, supportedArguments, i, candidates);

                i++;
            }

            // Validate if all mandatory arguments have been assigned
            ValidateMandatoryArguments(effectiveCommandName, supportedArguments);

            return supportedArguments;
        }

        /// <summary>
        /// Finds candidate arguments matching the user input
        /// </summary>
        private static List<Argument> FindCandidateArguments(string[] userArguments, ArgumentList supportedArguments, bool isArgFlag, string cleanInputArg)
        {
            List<Argument> candidates = new List<Argument>(userArguments.Length);
            foreach (Argument destArg in supportedArguments)
            {
                // Skip arguments which have already been assigned
                if (destArg.IsSet)
                    continue;

                // Clone the argument using its polymorphic Clone implementation
                Argument clonedArg = destArg.Clone();
                if (clonedArg is null)
                    throw new NoPowerShellException("Unexpected argument type");

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
                        throw new NoPowerShellException("Unexpected positional argument type");
                }
            }

            return candidates;
        }

        /// <summary>
        /// Handles potentially ambiguous parameters
        /// </summary>
        private static void HandleAmbiguousParameters(string commandName, List<Argument> candidates, string cleanInputArg)
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
                    commandName,
                    $"Parameter cannot be processed because the parameter name '{cleanInputArg}' is ambiguous. Possible matches include: -{string.Join(" -", paramNames)}."
                );
            }
        }

        /// <summary>
        /// Prioritizes mandatory candidates over optional ones
        /// </summary>
        private static List<Argument> PrioritizeMandatoryCandidates(List<Argument> candidates)
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
        private static int AssignValueToArgument(string commandName, string[] userArguments, ArgumentList supportedArguments, int currentIndex, List<Argument> candidates)
        {
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
                    currentIndex = AssignIntArgumentValue(commandName, userArguments, supportedArguments, currentIndex, intArg);
                    assignedValue = true;
                    break;
                }
                else if (a is StringArgument stringArg)
                {
                    if (stringArg.Value != null && !stringArg.IsDefaultValue)
                        continue;

                    currentIndex = AssignStringArgumentValue(commandName, userArguments, supportedArguments, currentIndex, stringArg);
                    assignedValue = true;
                    break;
                }
            }

            if (!assignedValue)
                throw new NoPowerShellException(
                    $@"{commandName}: Failed to assign value to parameter. This can be because of:
- A missing parameter name (e.g. 'MyValue' was used instead of '-Name MyValue')
- Duplicate arguments (e.g. '-Name MyValue -Name MyValue2')
- A missing pipe (' | ')"
                );

            return currentIndex;
        }

        /// <summary>
        /// Assigns a value to a boolean argument
        /// </summary>
        private static bool AssignBoolArgumentValue(ArgumentList supportedArguments, BoolArgument boolArg)
        {
            boolArg.Value = true;

            // Update original ArgumentList with the value
            var originalArg = supportedArguments.Get<BoolArgument>(boolArg.Name);
            if (originalArg == null)
                throw new NoPowerShellException($"Unexpected: bool argument '{boolArg.Name}' not found in supported arguments.");

            originalArg.Value = boolArg.Value;

            return true;
        }

        /// <summary>
        /// Assigns a value to an integer argument
        /// </summary>
        private static int AssignIntArgumentValue(string commandName, string[] userArguments, ArgumentList supportedArguments, int currentIndex, IntegerArgument intArg)
        {
            // Ensure a value follows the parameter
            if (currentIndex + 1 >= userArguments.Length)
            {
                throw new ParameterBindingException(
                    commandName,
                    $"A value for parameter '{intArg.Name}' is missing.");
            }

            // TODO: Currently simply overwrites the value if it is already set
            // Example: Get-ChildItem -Depth 1 -Depth 2
            intArg.Value = Convert.ToInt32(userArguments[++currentIndex]);

            // Update original ArgumentList with the value
            var originalArg = supportedArguments.Get<IntegerArgument>(intArg.Name);
            if (originalArg == null)
                throw new NoPowerShellException($"Unexpected: integer argument '{intArg.Name}' not found in supported arguments.");

            originalArg.Value = intArg.Value;
            originalArg.DashArgumentNameSkipUsed = true;

            return currentIndex;
        }

        /// <summary>
        /// Assigns a value to a string argument
        /// </summary>
        private static int AssignStringArgumentValue(string commandName, string[] userArguments, ArgumentList supportedArguments, int currentIndex, StringArgument stringArg)
        {
            // Positional StringArgument which requires a value
            if (!stringArg.DashArgumentNameSkipUsed)
                currentIndex++;

            if (currentIndex >= userArguments.Length)
            {
                throw new ParameterBindingException(
                    commandName,
                    $"A value for parameter '{stringArg.Name}' is missing.");
            }

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
                {
                    break;
                }
            }

            bool onlyArgument = strbargs.Length == 0;
            string strargs = strbargs.Append(userArguments[currentIndex]).ToString();

            // Array where current component is last one
            if (!onlyArgument)
                currentIndex++;

            stringArg.Value = UnescapeString(strargs);

            // Update original ArgumentList with the value and set the DashArgumentNameSkipUsed flag
            var originalArg = supportedArguments.Get<StringArgument>(stringArg.Name);
            if (originalArg == null)
                throw new NoPowerShellException($"Unexpected: string argument '{stringArg.Name}' not found in supported arguments.");

            originalArg.Value = stringArg.Value;
            originalArg.DashArgumentNameSkipUsed = true;

            return currentIndex;
        }

        /// <summary>
        /// Validates that all mandatory arguments have been assigned a value
        /// </summary>
        private static void ValidateMandatoryArguments(string commandName, ArgumentList supportedArguments)
        {
            foreach (Argument arg in supportedArguments)
            {
                if (!arg.IsOptionalArgument && !arg.IsSet)
                {
                    throw new NoPowerShellException($"{commandName}: Mandatory parameter '{arg.Name}' is missing.");
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
        internal static string UnescapeString(string input)
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
    }
}
