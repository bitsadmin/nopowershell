#if MAJOR1 || MAJOR2 || MAJOR3 || (MAJOR4 && MINOR0)
#warning Compress-Archive requires at least .NET 4.5
#else
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;
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
        public CompressArchiveCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string destinationPath = _arguments.Get<StringArgument>("DestinationPath").Value;
            string compressionLevel = _arguments.Get<StringArgument>("CompressionLevel").Value;
            CompressionLevel cl = CompressionLevel.Optimal;
            bool containsWildcard = path.IndexOfAny(new char[] { '*', '?' }) >= 0;

            // Determine compression level
            switch (compressionLevel.ToLowerInvariant())
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

            if (containsWildcard)
            {
                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                    directory = Directory.GetCurrentDirectory();

                string searchPattern = Path.GetFileName(path);
                if (string.IsNullOrEmpty(searchPattern))
                    searchPattern = "*";

                if (!Directory.Exists(directory))
                    throw new ItemNotFoundException(path);

                string[] files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                    throw new ItemNotFoundException(path);

                using (FileStream fs = new FileStream(destinationPath, FileMode.Create))
                using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    foreach (string file in files)
                    {
                        arch.CreateEntryFromFile(file, Path.GetFileName(file), cl);
                    }
                }
            }
            else
            {
                // Compress directory
                if (Directory.Exists(path))
                {
                    ZipFile.CreateFromDirectory(path, destinationPath, cl, false);
                }

                // Compress file
                else if (File.Exists(path))
                {
                    FileInfo fi = new FileInfo(path);

                    using (FileStream fs = new FileStream(destinationPath, FileMode.Create))
                    using (ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create))
                    {
                        arch.CreateEntryFromFile(path, fi.Name, cl);
                    }
                }

                else
                    throw new ItemNotFoundException(path);
            }

            // Return resulting filename
            _results.Add(
                new ResultRecord()
                {
                    { "Path", destinationPath }
                }
            );
            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Compress-Archive",
            "zip" // Unofficial
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Path"),
            new StringArgument("DestinationPath"),
            new StringArgument("CompressionLevel", "Optimal")
        };

        public static new string Synopsis => "Creates an archive, or zipped file, from specified files and folders.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry
            (
                "Compress folder to zip",
                new List<string>()
                {
                    "Compress-Archive -Path C:\\MyFolder -DestinationPath C:\\MyFolder.zip",
                    "zip C:\\MyFolder C:\\MyFolder.zip"
                }
            )
        };
    }
}
#endif