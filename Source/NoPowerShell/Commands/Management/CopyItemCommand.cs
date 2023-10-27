using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
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
            // Obtain source and destination
            string path = _arguments.Get<StringArgument>("Path").Value;
            string destination = _arguments.Get<StringArgument>("Destination").Value;
            bool force = _arguments.Get<BoolArgument>("Force").Value;

            // Determine if provided path is a file or a directory
            FileAttributes attr = File.GetAttributes(path);

            // Directory
            if((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DirectoryCopy(path, destination, force);
            }
            // File
            else
            {
                File.Copy(path, destination, force);
            }

            return _results;
        }

        // Slightly modified version of:
        // https://docs.microsoft.com/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool force)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string destPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(destPath, force);
            }

            // Copy subdirectories and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, force);
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
