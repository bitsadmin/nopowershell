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
    public class RemoveItemCommand : PSCommand
    {
        public RemoveItemCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect common parameters
            base.Execute();

            // Obtain source and destination
            string path = _arguments.Get<StringArgument>("Path").Value;
            bool force = _arguments.Get<BoolArgument>("Force").Value;
            bool recurse = _arguments.Get<BoolArgument>("Recurse").Value;

            // Determine if provided path is a file or a directory
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new NoPowerShellException("Cannot find path '{0}' because it does not exist.", path);

            FileAttributes attr = File.GetAttributes(path);

            // Directory
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DirectoryDelete(path, recurse, force, verbose);

                if (verbose)
                    Console.WriteLine("Removing directory: {0}", path);

                Directory.Delete(path);
            }
            // File
            else
            {
                // Remove readonly attribute
                if(force)
                    File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);

                if(verbose)
                    Console.WriteLine("Removing file: {0}", path);

                File.Delete(path);
            }

            return _results;
        }

        // Inspired by:
        // https://docs.microsoft.com/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryDelete(string dirName, bool recurse, bool force, bool verbose)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(dirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (dirs.Length > 0 && !recurse)
                throw new NoPowerShellException(string.Format("The item at {0} has children and the Recurse parameter was not specified. Nothing has been removed.", dirName));

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                // Remove readonly attribute
                if (force)
                    File.SetAttributes(file.FullName, file.Attributes & ~FileAttributes.ReadOnly);

                if(verbose)
                    Console.WriteLine("Removing file: {0}", file.FullName);

                file.Delete();
            }

            // Remove subdirectories and their contents if Recurse flag is set
            if (recurse)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    DirectoryDelete(subdir.FullName, recurse, force, verbose);

                    if (verbose)
                        Console.WriteLine("Removing directory: {0}", subdir.FullName);

                    subdir.Delete();
                }
            }
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Remove-Item", "del", "erase", "rd", "ri", "rm", "rmdir" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new BoolArgument("Force"),
                    new BoolArgument("Recurse")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Deletes files and folders."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Delete a file",
                        new List<string>()
                        {
                            "Remove-Item C:\\tmp\\MyFile.txt",
                            "rm C:\\tmp\\MyFile.txt"
                        }
                    ),
                    new ExampleEntry("Delete a read-only file", "Remove-Item -Force C:\\Tmp\\MyFile.txt"),
                    new ExampleEntry("Recursively delete a folder", "Remove-Item -Recurse C:\\Tmp\\MyTools\\")
                };
            }
        }
    }
}
