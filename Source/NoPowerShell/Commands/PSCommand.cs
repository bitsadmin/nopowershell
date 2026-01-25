using System;
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

        /// <summary>
        /// Construct a new PSCommand parsing the provided arguments using the provided list of arguments supported by this cmdlet
        /// </summary>
        /// <param name="userArguments">Arguments to be parsed</param>
        /// <param name="supportedArguments">Arguments supported by this cmdlet</param>
        public PSCommand(string[] userArguments)
        {
            _arguments = ParseArguments(userArguments, SupportedArguments);
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
            return ArgumentParser.ParseArguments(userArguments, supportedArguments, ToString());
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
            return ArgumentParser.UnescapeString(input);
        }

        /// <summary>
        /// Implementation of the cmdlet.
        /// When calling the base class, it obtains the values from the ComputerName, Username and Password parameters and populates the corresponding variables.
        /// </summary>
        /// <param name="pipeIn">Output from previous command in pipe</param>
        /// <returns></returns>
        public virtual CommandResult Execute(CommandResult pipeIn = null)
        {
            return pipeIn;
        }

        /// <summary>
        /// Command + aliases of PSCommand. Is used for displaying help and to determine which command a user wants to execute.
        /// </summary>
        public virtual CaseInsensitiveList Aliases => throw new InvalidOperationException("The Aliases attribute should be overridden");

        /// <summary>
        /// Name of cmdlet
        /// </summary>
        public string Command => Aliases[0]; // First command in list of Aliases must always be the full cmdlet

        /// <summary>
        /// List of supported arguments. Order of the arguments will be reflected in the help (Get-Command).
        /// </summary>
        //public virtual ArgumentList SupportedArguments => throw new InvalidOperationException("The SupportedArguments attribute should be overridden");
        public ArgumentList SupportedArguments
        {
            get
            {
                PropertyInfo supportedArgumentsProperty = this.GetType().GetProperty("SupportedArguments", BindingFlags.Static | BindingFlags.Public);
                ArgumentList supportedArguments = (ArgumentList)supportedArgumentsProperty.GetValue(null, null);
                return supportedArguments;
            }
        }

        public virtual string Synopsis => throw new InvalidOperationException("The Synopsis attribute should be overridden");

        public virtual ExampleEntries Examples => new ExampleEntries();

        /// <summary>
        /// Gets the final parsed arguments for this command.
        /// Exposed primarily for testing and diagnostics.
        /// </summary>
        public ArgumentList ParsedArguments => _arguments;

        public override string ToString()
        {
            PropertyInfo aliasesProperty = this.GetType().GetProperty("Aliases", BindingFlags.Static | BindingFlags.Public);
            CaseInsensitiveList aliases = (CaseInsensitiveList)aliasesProperty.GetValue(null, null);

            return aliases[0];
        }
    }
}
