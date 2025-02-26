using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class CopyItemCommand : PSCommand
    {
        public CopyItemCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect common parameters
            base.Execute();

            // Obtain source and destination
            string path = _arguments.Get<StringArgument>("Path").Value;
            string destination = _arguments.Get<StringArgument>("Destination").Value;
            bool recurse = _arguments.Get<BoolArgument>("Recurse").Value;
            bool force = _arguments.Get<BoolArgument>("Force").Value;

            // Determine if provided path is a file or a directory
            FileAttributes attr = File.GetAttributes(path);

            // Directory
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DirectoryCopy(path, destination, force, recurse, verbose);
            }
            // File
            else
            {
                if (verbose)
                    Console.WriteLine($"Copying {path} to {destination}");

                File.Copy(path, destination, force);
            }

            return _results;
        }

        // Slightly modified version of:
        // https://docs.microsoft.com/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool force, bool recurse, bool verbose)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                if (verbose)
                    Console.WriteLine($"Creating directory: {destDirName}");

                Directory.CreateDirectory(destDirName);
            }
            // If it already exists, create a subdirectory
            else
            {
                destDirName = Path.Combine(destDirName, dir.Name);

                if (Directory.Exists(destDirName))
                {
                    Console.WriteLine($"Copy-Item: An item with the specified name {destDirName} already exists.");
                }
                else
                {
                    if (verbose)
                        Console.WriteLine($"Creating directory: {destDirName}");

                    Directory.CreateDirectory(destDirName);
                }
            }

            // Get the files in the directory and copy them to the new location.
            if (recurse)
            {
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string destPath = Path.Combine(destDirName, file.Name);
                    if (verbose)
                        Console.WriteLine($"Copying {file.FullName} to {destPath}");

                    file.CopyTo(destPath, force);
                }

                // Copy subdirectories and their contents to new location.
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, force, recurse, verbose);
                }
            }
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Copy-Item", "copy", "cp", "cpi" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("Destination"),
                    new BoolArgument("Recurse"),
                    new BoolArgument("Force")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Copies an item from one location to another."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Copy file from one location to another",
                        new List<string>()
                        {
                            "Copy-Item C:\\Tmp\\nc.exe C:\\Windows\\System32\\nc.exe",
                            "copy C:\\Tmp\\nc.exe C:\\Windows\\System32\\nc.exe",
                        }
                    ),
                    new ExampleEntry("Copy folder", "copy C:\\Tmp\\MyFolder C:\\Tmp\\MyFolderBackup")
                };
            }
        }
    }
}
