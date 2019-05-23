#if MAJOR1 || MAJOR2 || MAJOR3 || (MAJOR4 && MINOR0)
#warning Expand-Archive requires at least .NET 4.5
#else
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.IO;
using System.IO.Compression;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Archive
{
    public class ExpandArchiveCommand : PSCommand
    {
        public ExpandArchiveCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string destinationPath = _arguments.Get<StringArgument>("DestinationPath").Value;

            // Validate path
            if (!File.Exists(path))
                throw new ItemNotFoundException(path);

            // Determine destination path if not specified
            if (string.IsNullOrEmpty(destinationPath))
                destinationPath = Path.GetFullPath(Path.Combine(".\\", Path.GetFileNameWithoutExtension(path)));

            // Extract
            ZipFile.ExtractToDirectory(path, destinationPath);

            // Return resulting filename
            _results.Add(
                new ResultRecord()
                {
                    { "Path", destinationPath }
                }
            );
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get
            {
                return new CaseInsensitiveList()
                {
                    "Expand-Archive",
                    "unzip" // Unofficial
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("DestinationPath")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Extracts files from a specified archive (zipped) file."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Extract zip", "Expand-Archive -Path C:\\MyArchive.zip -DestinationPath C:\\Extracted"),
                    new ExampleEntry("Extract zip current directory", "unzip C:\\MyArchive.zip"),
                };
            }
        }
    }
}
#endif