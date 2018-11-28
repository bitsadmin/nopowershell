using System;
using System.Collections.Generic;
using NoPowerShell.Commands;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    class PipeParser
    {
        public static List<PSCommand> ParseArguments(string[] args, Dictionary<Type, CaseInsensitiveList> commandTypes)
        {
            List<List<string>> parsedPipes = new List<List<string>>();
            List<string> currentPipe = new List<string>();

            // Split pipes
            foreach (string arg in args)
            {
                if (arg == "|")
                {
                    parsedPipes.Add(currentPipe);
                    currentPipe = new List<string>();
                }
                else
                {
                    currentPipe.Add(arg);
                }
            }
            parsedPipes.Add(currentPipe);

            // Parse commands between pipes
            List<PSCommand> allCommands = new List<PSCommand>(parsedPipes.Count);
            foreach (List<string> pipe in parsedPipes)
            {
                string command = pipe[0].ToLowerInvariant();
                string[] pipeargs = pipe.GetRange(1, pipe.Count - 1).ToArray();

                // Locate the command in the aliases of the available commands
                bool foundMatchingCommand = false;
                foreach (KeyValuePair<Type, CaseInsensitiveList> commandType in commandTypes)
                {
                    if (commandType.Value.Contains(command))
                    {
                        object[] parameters = new object[] { pipeargs };
                        allCommands.Add((PSCommand)Activator.CreateInstance(commandType.Key, parameters));
                        foundMatchingCommand = true;
                        break;
                    }
                }

                if (!foundMatchingCommand)
                    throw new ArgumentException("Unknown command");
            }

            return allCommands;
        }
    }
}
