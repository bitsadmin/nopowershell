#if MAJOR1 || MAJOR2 || MAJOR3 || (MAJOR4 && MINOR0)
#warning Compress-Archive requires at least .NET 4.5
#else
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.IO.Compression;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Archive
{
    public class CompressArchiveCommand : PSCommand
    {
        public CompressArchiveCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string destinationPath = _arguments.Get<StringArgument>("DestinationPath").Value;
            string compressionLevel = _arguments.Get<StringArgument>("CompressionLevel").Value;
            CompressionLevel cl = CompressionLevel.Optimal;

            // Determine compression level
            switch(compressionLevel.ToLowerInvariant())
            {
                case "optimal":
                    cl = CompressionLevel.Optimal;
                    break;
                case "nocompression":
                    cl = CompressionLevel.NoCompression;
                    break;
                case "fastest":
                    cl = CompressionLevel.Fastest;
                    break;
                default:
                    throw new ArgumentException(string.Format("Unknown compression level: {0}. Possible options: Optimal, NoCompression, Fastest.", compressionLevel));
            }

            // Compress
            ZipFile.CreateFromDirectory(path, destinationPath, cl, false);

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
                    "Compress-Archive",
                    "zip" // Unofficial
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
                    new StringArgument("DestinationPath"),
                    new StringArgument("CompressionLevel", "Optimal")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Creates an archive, or zipped file, from specified files and folders."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Compress folder to zip", "Compress-Archive -Path C:\\MyFolder -DestinationPath C:\\MyFolder.zip"),
                };
            }
        }
    }
}
#endif