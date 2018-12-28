using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Collections;

/*
Author: @bitsadmin
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

            // Registry:
            //     HKLM:\
            //     HKCU:\
            //     HKCR:\
            //     HKU:\
            RegistryKey root = ProviderHelper.GetRegistryKey(ref path);
            if (root != null)
                _results = BrowseRegistry(root, path, includeHidden);

            // Environment
            //     env:
            //     env:systemroot
            else if (path.ToUpperInvariant().StartsWith("ENV"))
                _results = BrowseEnvironment(path);
            
            // Filesystem:
            //     \
            //     ..\
            //     D:\
            else
                _results = BrowseFilesystem(path, recurse, includeHidden, searchPattern);

            return _results;
        }

        private static CommandResult BrowseRegistry(RegistryKey root, string path, bool includeHidden)
        {
            CommandResult results = new CommandResult();

            RegistryKey key = root.OpenSubKey(path);
            foreach (string subkey in key.GetSubKeyNames())
            {
                results.Add(
                    new ResultRecord()
                    {
                        { "Name", subkey }
                    }
                );
            }

            return results;
        }

        private CommandResult BrowseEnvironment(string path)
        {
            CommandResult results = new CommandResult();

            string[] selection = path.Split(':');
            string filter = null;
            if (selection.Length > 1)
                filter = selection[1];

            Hashtable variables = new Hashtable((Hashtable)Environment.GetEnvironmentVariables(), StringComparer.InvariantCultureIgnoreCase);

            // Obtain specific variable
            if (!string.IsNullOrEmpty(filter))
            {
                if (!variables.ContainsKey(filter))
                    throw new Exception(string.Format("Cannot find path '{0}' because it does not exist.", filter));

                results.Add(
                    new ResultRecord()
                    {
                        { "Name", filter },
                        { "Value", variables[filter].ToString() }
                    }
                );
            }
            // Obtain all variables
            else
            {
                foreach(DictionaryEntry variable in variables)
                {
                    results.Add(
                        new ResultRecord()
                        {
                            { "Name", variable.Key.ToString() },
                            { "Value", variable.Value.ToString() }
                        }
                    );
                }
            }

            return results;
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
            catch (UnauthorizedAccessException)
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
            if (recurse)
            {
                foreach (DirectoryInfo subDir in directories)
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
