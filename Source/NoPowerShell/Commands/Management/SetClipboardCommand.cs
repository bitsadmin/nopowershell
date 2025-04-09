using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.Windows.Forms;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class SetClipboardCommand : PSCommand
    {
        public SetClipboardCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            bool append = _arguments.Get<BoolArgument>("Append").Value;
            string value = _arguments.Get<StringArgument>("Value").Value;
            bool passthru = _arguments.Get<BoolArgument>("PassThru").Value;

            string contents = GetContents(pipeIn, value);

            if (append)
            {
                string currentText = Clipboard.GetText();
                currentText += contents;
                contents = currentText;
            }

            if (string.IsNullOrEmpty(contents))
                Clipboard.Clear();
            else
                Clipboard.SetText(contents);

            // Set clipboard contents as result if the -PassThru parameter is provided
            // Otherwise simply the empty _results set will be returned
            if (passthru)
            {
                _results = new CommandResult(1)
                {
                    new ResultRecord(1)
                    {
                        { "Data", contents }
                    }
                };
            }

            return _results;
        }

        private string GetContents(CommandResult pipeIn, string value)
        {
            string pipeAsString = PipelineHelper.PipeToString(pipeIn);

            if (!string.IsNullOrEmpty(pipeAsString))
            {
                if (!string.IsNullOrEmpty(value))
                    throw new NoPowerShellException("Either use pipeline input or parameter input, not both");

                return pipeAsString;
            }
            else
                return value;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Set-Clipboard", "scb" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Value"),
                    new BoolArgument("Append"),
                    new BoolArgument("PassThru")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Sets the current Windows clipboard entry."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Put string on clipboard",
                        new List<string>()
                        {
                            "Set-Clipboard -Value \"You have been PWNED!\"",
                            "scb \"You have been PWNED!\""
                        }
                    ),
                    new ExampleEntry("Clear the clipboard", "Set-Clipboard"),
                    new ExampleEntry("Place output of command on clipboard", "Get-Process | Set-Clipboard")
                };
            }
        }
    }
}
