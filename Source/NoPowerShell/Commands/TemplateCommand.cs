using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class TemplateCommand : PSCommand
    {
        public TemplateCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            // Will contain all of the arguments from the 'ArgumentList Arguments' below
            bool myFlag = _arguments.Get<BoolArgument>("MyFlag").Value;
            int myInteger = _arguments.Get<IntegerArgument>("MyInteger").Value;
            string myString = _arguments.Get<StringArgument>("MyString").Value;

            // The following (optional) parameters are always available,
            // no need to add them to the SupportedArguments below
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            // Write your code here, storing the output in attributename-value pairs
            // Example of resulting table:
            // Attribute1                    Attribute2
            // ----------                    ----------
            // Line00-Attribute1-Hello World Line00-Attribute2-Hello World
            // Line01-Attribute1-Hello World Line01-Attribute2-Hello World
            // Line02-Attribute1-Hello World Line02-Attribute2-Hello World
            // Line03-Attribute1-Hello World Line03-Attribute2-Hello World
            // Line04-Attribute1-Hello World Line04-Attribute2-Hello World
            // etc.

            if (!myFlag)
            {
                for (int i = 0; i < myInteger; i++)
                {
                    _results.Add(
                        new ResultRecord()
                        {
                            { "Attribute1", string.Format("Line{0:D2}-Attribute1-{1}", i, myString) },
                            { "Attribute2", string.Format("Line{0:D2}-Attribute2-{1}", i, myString) }
                            //{ "AttributeN", string.Format("Line{0}-AttributeN-{1}", i, myString) }
                        }
                    );
                }
            }
            else
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { string.Empty, "MyFlag flag has been set." }
                    }
                );
            }

            // Always return the results so the output can be used by the next command in the pipeline
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-TemplateCommand", "gtc" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("MyFlag"),
                    new IntegerArgument("MyInteger", 5, true),
                    new StringArgument("MyString", "Hello World")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "This template shows how easy it is to develop new NoPowerShell cmdlets."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("These entries show up when executing the 'Get-Help Get-TemplateCommand' command", "Get-TemplateCommand -MyFlag"),
                    new ExampleEntry
                    (
                        "This is another example with two related or equivalent examples",
                        new List<string>()
                        {
                            "gtc \"Bye PowerShell\" -MyInteger 30 | ? Attribute2 -Like Line1* | select Attribute2 | fl",
                            "Get-TemplateCommand -MyInteger 10 \"Bye PowerShell\""
                        }
                    )
                };
            }
        }
    }
}
