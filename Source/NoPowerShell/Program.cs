using System;
using System.Collections.Generic;
using System.Linq;
using NoPowerShell.Commands;
using NoPowerShell.Commands.Core;
using NoPowerShell.Commands.Utility;
using NoPowerShell.HelperClasses;
#if BOFBUILD
using BOFNET;
#endif

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell
{
#if BOFBUILD
    public class Program : BeaconObject
    {
        public Program(BeaconApi api) : base(api) { }

        public override void Go(string[] args)
        {
#else
    partial class Program
    {
        [STAThread] // Required for the *-Clipboard cmdlets
        public static void Main(string[] args)
        {
#endif
            // Display commandline arguments if verbose output is enabled
            if (args.Any(x => x.Equals("-Verbose", StringComparison.InvariantCultureIgnoreCase)))
                Console.WriteLine("Commandline: '{0}'", string.Join("' '", args));

            // Using reflection determine available commands
            Dictionary<Type, CaseInsensitiveList> availableCommands = ReflectionHelper.GetCommands();
            List<PSCommand> userCommands = null;

            // If no arguments are provided to the executable, show help
            if (args.Length == 0)
            {
                Console.WriteLine("== NoPowerShell v{0} ==\r\nWebsite: {1}\r\n{2}", VERSION, WEBSITE, USAGE);
                userCommands = new List<PSCommand>(1) { new GetCommandCommand(null) };
            }
            // Parse pipes in commandline arguments and commands within pipes
            else
            {
                string error = null;

                try
                {
                    userCommands = PipeParser.ParseArguments(args, availableCommands);
                }
                catch (CommandNotFoundException ex) when (ex is ParameterBindingException || ex is DuplicateParameterException)
                {
                    error = ex.Message;
                }
                catch (CommandNotFoundException ex)
                {
                    error = string.Join("", new string[] { ex.Message, HELP });
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                if (error != null)
                {
#if BOFBUILD
                    BeaconConsole.WriteLine(error);
#else
                    WriteError(error);
                    return;
#endif
                }
            }

            // Add output to console if no explicit output is provided
            Type lastCommand = userCommands[userCommands.Count - 1].GetType();
            bool justOutput = false;
            if (lastCommand != typeof(FormatListCommand) && lastCommand != typeof(FormatTableCommand))
                justOutput = true;

            CommandResult result = null;
#if DEBUG
            // Execute commands in pipeline
            foreach (PSCommand command in userCommands)
            {
                result = command.Execute(result);
            }
#else
            PSCommand mostRecentCommand = null;
            try
            {
                // Execute commands in pipeline
                foreach (PSCommand command in userCommands)
                {
                    mostRecentCommand = command;
                    result = command.Execute(result);
                }
            }
            catch (NoPowerShellException e)
            {
                WriteError(string.Format("{0} : {1}", mostRecentCommand.ToString(), e.Message));
                return;
            }
            catch (Exception e)
            {
                WriteError(string.Format("{0} : {1}", mostRecentCommand.ToString(), e.ToString()));
                return;
            }
#endif

            // Output to screen
            if (justOutput)
            {
                string output = ResultPrinter.OutputResults(result);

#if BOFBUILD
                BeaconConsole.WriteLine(output);
#else
                Console.Write(output);
#endif
            }
        }

        public static void WriteError(string error, params object[] args)
        {
            WriteColored(null, ConsoleColor.Black, ConsoleColor.Red, error, args);
        }

        public static void WriteWarning(string warning, params object[] args)
        {
            WriteColored("WARNING", ConsoleColor.Black, ConsoleColor.Yellow, warning, args);
        }

        public static void WriteVerbose(string warning, params object[] args)
        {
            WriteColored("VERBOSE", ConsoleColor.Black, ConsoleColor.Yellow, warning, args);
        }

        public static void WriteColored(string prefix, ConsoleColor background, ConsoleColor foreground, string message, params object[] args)
        {
            // Save existing color
            ConsoleColor BackgroundColor = Console.BackgroundColor;
            ConsoleColor ForegroundColor = Console.ForegroundColor;

            // Change color to error text
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;

            // Compose message
            string line = null;
            if (!string.IsNullOrEmpty(prefix))
                line = string.Format("{0}: {1}", prefix, message);
            else
                line = message;

            // Display message
            Console.WriteLine(line, args);

            // Revert colors
            Console.BackgroundColor = BackgroundColor;
            Console.ForegroundColor = ForegroundColor;
        }

        public static readonly string VERSION = "1.26";
        public static readonly string WEBSITE = "https://github.com/bitsadmin/nopowershell";
#if !DLLBUILD
        private static readonly string USAGE = "Usage: NoPowerShell.exe [Command] [Parameters] | [Command2] [Parameters2] etc.\r\n";
        private static readonly string HELP = "\r\nExecute NoPowerShell without parameters to list all available cmdlets.";
#else
        private static readonly string USAGE = "";
        private static readonly string HELP = " Type 'help' to list all available cmdlets.";
#endif
    }
}
