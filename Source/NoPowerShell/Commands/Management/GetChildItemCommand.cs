using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetChildItemCommand : PSCommand
    {
        public GetChildItemCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            bool includeHidden = _arguments.Get<BoolArgument>("Force").Value;
            string path = _arguments.Get<StringArgument>("Path").Value;
            bool recurse = _arguments.Get<BoolArgument>("Recurse").Value;
            string searchPattern = _arguments.Get<StringArgument>("Include").Value;
            string checkPath = path.ToUpperInvariant();

            // Registry:
            //     HKLM:\
            //     HKCU:\
            //     HKCR:\
            //     HKU:\
            if (checkPath.StartsWith("HKLM:") || checkPath.StartsWith("HKCU:") || checkPath.StartsWith("HKCR:") || checkPath.StartsWith("HKU:"))
                _results = BrowseRegistry(path, includeHidden);
            
            // Filesystem:
            //     \
            //     ..\
            //     D:\
            else
                _results = BrowseFilesystem(path, recurse, includeHidden, searchPattern);

            return _results;
        }
        private CommandResult BrowseRegistry(string path, bool includeHidden)
        {
            RegistryKey root = null;
            string newPath = string.Empty;
            path = path.ToUpperInvariant();
            if (path.StartsWith("HKLM:"))
            {
                root = Registry.LocalMachine;
                newPath = path.Replace("HKLM:", string.Empty);
            }
            else if (path.StartsWith("HKCU:"))
            {
                root = Registry.CurrentUser;
                newPath = path.Replace("HKCU:", string.Empty);
            }
            else if (path.StartsWith("HKCR:"))
            {
                root = Registry.ClassesRoot;
                newPath = path.Replace("HKCR:", string.Empty);
            }
            else if (path.StartsWith("HKU:"))
            {
                root = Registry.Users;
                newPath = path.Replace("HKU:", string.Empty);
            }
            else
                throw new InvalidOperationException("Unknown registry path.");

            if (newPath.StartsWith(@"\"))
                newPath = newPath.Substring(1);

            RegistryKey key = root.OpenSubKey(newPath);
            foreach (string subkey in key.GetSubKeyNames())
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", subkey }
                    }
                );
            }

            return _results;
        }

        public static CommandResult BrowseFilesystem(string path, bool recurse, bool includeHidden, string searchPattern)
        {
            CommandResult results = new CommandResult();

            DirectoryInfo gciDir = new DirectoryInfo(path);

            // TODO: Follow symlinks. Skipping them for now
            if ((gciDir.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return results;

            DirectoryInfo[] directories = null;
            try
            {
                 directories = gciDir.GetDirectories(recurse ? "*" : searchPattern);
            }
            catch(UnauthorizedAccessException)
            {
                Console.WriteLine("Unauthorized to access \"{0}\"", path);
                return results;
            }

            FileInfo[] files = gciDir.GetFiles(searchPattern);

            // Enumerate directories
            foreach (DirectoryInfo dir in directories)
            {
                if (!includeHidden && ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                    continue;

                // Don't show directories if -Recurse and an -Include filter is set
                if (recurse && !string.IsNullOrEmpty(searchPattern))
                    continue;
                
                ResultRecord currentDir = new ResultRecord()
                {
                    { "Mode", GetModeFlags(dir) },
                    { "LastWriteTime", dir.LastWriteTime.ToString() },
                    { "Length", string.Empty },
                    { "Name", dir.Name }
                };

                // If recursive, also the directory name is needed
                if (recurse)
                    currentDir.Add("Directory", dir.FullName);

                results.Add(currentDir);
            }

            // Enumerate files
            foreach (FileInfo file in files)
            {
                if (!includeHidden && ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                    continue;

                ResultRecord currentFile = new ResultRecord()
                {
                    { "Mode", GetModeFlags(file) },
                    { "LastWriteTime", file.LastWriteTime.ToString() },
                    { "Length", file.Length.ToString() },
                    { "Name", file.Name }
                };

                // If recursive, also the directory name is needed
                if (recurse)
                    currentFile.Add("Directory", file.Directory.FullName);

                results.Add(currentFile);
            }

            // After adding folders and files in current directory, go depth first
            if(recurse)
            {
                foreach(DirectoryInfo subDir in directories)
                {
                    // Skip hidden directories in case -Force parameter is not provided
                    if ((subDir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden && !includeHidden)
                        continue;

                    CommandResult currentDir = BrowseFilesystem(subDir.FullName, recurse, includeHidden, searchPattern);
                    results.AddRange(currentDir);
                }
            }

            return results;
        }

        private static string GetModeFlags(FileSystemInfo f)
        {
            StringBuilder sb = new StringBuilder(6);

            sb.Append((f.Attributes & FileAttributes.Directory) == FileAttributes.Directory ? "d" : "-");
            sb.Append((f.Attributes & FileAttributes.Archive) == FileAttributes.Archive ? "a" : "-");
            sb.Append((f.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ? "r" : "-");
            sb.Append((f.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ? "h" : "-");
            sb.Append((f.Attributes & FileAttributes.System) == FileAttributes.System ? "s" : "-");
            sb.Append((f.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint ? "l" : "-");

            return sb.ToString();
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ChildItem", "gci", "ls", "dir" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path", "."),
                    new BoolArgument("Force") ,
                    new BoolArgument("Recurse"),
                    new StringArgument("Include", "*", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the files and folders in a file system drive."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Locate KeePass files in the C:\\Users\\ directory", "ls -Recurse -Force C:\\Users\\ -Include *.kdbx"),
                    new ExampleEntry("List the keys under the SOFTWARE key in the registry", "ls HKLM:\\SOFTWARE")
                };
            }
        }
    }
}
