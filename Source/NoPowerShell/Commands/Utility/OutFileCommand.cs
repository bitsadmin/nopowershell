using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.IO;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class OutFileCommand : PSCommand
    {
        public OutFileCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string filePath = _arguments.Get<StringArgument>("FilePath").Value;
            string encoding = _arguments.Get<StringArgument>("Encoding").Value;
            bool passthru = _arguments.Get<BoolArgument>("PassThru").Value;

            Encoding encodingType = Encoding.GetEncoding(encoding);

            // Serialize object to string
            string pipeAsString = PipelineHelper.PipeToString(pipeIn);

            // Ignore if empty
            if (string.IsNullOrEmpty(pipeAsString))
                return _results;

            // Store in file
            File.WriteAllText(filePath, pipeAsString, encodingType);

            // Empty result because it is written to a file
            if (passthru)
                _results = pipeIn;

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Out-File" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("FilePath", false),
                    new StringArgument("Encoding", "UTF-8"),
                    new BoolArgument("PassThru", false)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Sends output to a file."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Echo string to the console", "echo \"Hello Console!\""),
                    new ExampleEntry
                    (
                        "Create file hello.txt on the C: drive containing the \"Hello World!\" ASCII string",
                        new List<string>()
                        {
                            @"Write-Output ""Hello World!"" | Out-File -Encoding ASCII C:\hello.txt",
                            @"echo ""Hello World!"" | Out-File -Encoding ASCII C:\hello.txt"
                        }
                    )
                };
            }
        }
    }
}
