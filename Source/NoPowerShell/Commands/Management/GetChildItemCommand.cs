using Microsoft.Win32;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
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
            string literalPath = _arguments.Get<StringArgument>("LiteralPath").Value;
            bool recurse = _arguments.Get<BoolArgument>("Recurse").Value;
            int depth = _arguments.Get<IntegerArgument>("Depth").Value;
            string[] searchPatterns = _arguments.Get<StringArgument>("Include").Value.Split(new char[] { ',' });
            bool followSymlink = _arguments.Get<BoolArgument>("FollowSymlink").Value;
            bool useLiteralPath = false;

            // LiteralPath
            if (!string.IsNullOrEmpty(literalPath))
            {
                if (!string.IsNullOrEmpty(path) && path != ".")
                    throw new NoPowerShellException("Specify either -Path or -LiteralPath, not both");

                useLiteralPath = true;
                path = literalPath;
            }

            // If Depth is specified, it implies recursion
            if (depth != int.MaxValue)
                recurse = true;

            // Registry:
            //     HKLM:\
            //     HKCU:\
            //     HKCR:\
            //     HKU:\
            string registryPattern = @"^(HKLM|HKCU|HKCR|HKU):.*$";
            Regex registryRegex = new Regex(registryPattern);
            if (registryRegex.IsMatch(path))
            {
                RegistryHive root = RegistryHelper.GetRoot(ref path);
                _results = BrowseRegistry(root, path);
            }
            // Environment
            //     env:
            //     env:systemroot
            else if (path.ToUpperInvariant().StartsWith("ENV"))
            {
                _results = BrowseEnvironment(path);
            }
            // SMB share or full path:
            //     \\
            //     \\?\C:\
            //     \\?\UNC\MYSERVER\C$
            else if (path.StartsWith(@"\\"))
            {
                // Fix path to support long paths if not the literal path is used
                if(!useLiteralPath)
                {
                    // Full network path: \\MYSERVER -> \\?\UNC\MYSERVER
                    if (!path.ToUpperInvariant().StartsWith(@"\\?\UNC"))
                        path = path.Replace(@"\\", @"\\?\UNC\");
                }

                _results = BrowseFilesystem(path, recurse, depth, includeHidden, searchPatterns, useLiteralPath, followSymlink);
            }
            // Filesystem:
            //     \
            //     ..\
            //     D:\
            else
            {
                // Add \\?\ prefix to support long paths
                if (!useLiteralPath && !path.StartsWith(@"\\?\"))
                    path = @"\\?\" + path;

                _results = BrowseFilesystem(path, recurse, depth, includeHidden, searchPatterns, useLiteralPath, followSymlink);
            }

            return _results;
        }

        private static CommandResult BrowseRegistry(RegistryHive root, string path)
        {
            CommandResult results = new CommandResult();

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64))
            {
                using (RegistryKey key = baseKey.OpenSubKey(path))
                {
                    foreach (string subkey in key.GetSubKeyNames())
                    {
                        results.Add(
                            new ResultRecord()
                            {
                                { "Name", subkey }
                            }
                        );
                    }
                }
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

        public static CommandResult BrowseFilesystem(string path, bool recurse, int depth, bool includeHidden, string[] searchPatterns,
            bool useLiteralPath, bool followSymlink)
        {
            CommandResult results = new CommandResult();

            DirectoryInfo gciDir = new DirectoryInfo(path);

            if (!gciDir.Exists)
                throw new ItemNotFoundException(path);

            // Follow symlinks only if -FollowSymlink flag is specified, otherwise skip them
            if (!followSymlink && (gciDir.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                return results;

            // Display directory name
            if (!recurse)
                Console.WriteLine("\r\n    Directory: {0}\r\n", CleanupFullPath(gciDir.FullName, useLiteralPath));

            List<DirectoryInfo> directories = new List<DirectoryInfo>();
            try
            {
                if (recurse)
                    directories.AddRange(gciDir.GetDirectories("*"));
                else
                {
                    foreach (string pattern in searchPatterns)
                    {
                        directories.AddRange(gciDir.GetDirectories(pattern));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Program.WriteError("Access to the path '{0}' is denied.", CleanupFullPath(path, useLiteralPath));
                return results;
            }

            List<FileInfo> files = new List<FileInfo>();
            foreach (string pattern in searchPatterns)
            {
                files.AddRange(gciDir.GetFiles(pattern));
            }

            // Enumerate directories
            foreach (DirectoryInfo dir in directories)
            {
                if (!includeHidden && ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden))
                    continue;

                // Don't show directories if -Recurse and an -Include filter is set
                if (recurse && !string.IsNullOrEmpty(searchPatterns[0]))
                    continue;

                ResultRecord currentDir = new ResultRecord()
                {
                    { "Mode", GetModeFlags(dir) },
                    { "LastWriteTime", dir.LastWriteTime.ToFormattedString() },
                    { "Length", string.Empty },
                };

                // If -Recurse is set, show the full path, otherwise the name
                if (recurse)
                    currentDir.Add("FullName", CleanupFullPath(dir.FullName, useLiteralPath));
                else
                    currentDir.Add("Name", CleanupFullPath(dir.Name, useLiteralPath));

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
                    { "LastWriteTime", file.LastWriteTime.ToFormattedString() },
                    { "Length", file.Length.ToString() }
                };

                // If -Recurse is set, show the full path, otherwise the name
                if (recurse)
                    currentFile.Add("FullName", CleanupFullPath(file.FullName, useLiteralPath));
                else
                    currentFile.Add("Name", CleanupFullPath(file.Name, useLiteralPath));

                results.Add(currentFile);
            }

            // After adding folders and files in current directory, go depth first
            if (recurse && depth > 0)
            {
                foreach (DirectoryInfo subDir in directories)
                {
                    // Skip hidden directories in case -Force parameter is not provided
                    if ((subDir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden && !includeHidden)
                        continue;

                    CommandResult currentDir = BrowseFilesystem(subDir.FullName, recurse, depth - 1, includeHidden, searchPatterns, useLiteralPath, followSymlink);
                    results.AddRange(currentDir);
                }
            }

            return results;
        }

        private static string CleanupFullPath(string path, bool useLiteralPath)
        {
            if (useLiteralPath)
                return path;
            else
                return path
                    .Replace(@"\\?\UNC\", @"\\")
                    .Replace(@"\\?\", "");
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
                    new StringArgument("LiteralPath", true),
                    new BoolArgument("Force") ,
                    new BoolArgument("Recurse"),
                    new IntegerArgument("Depth", int.MaxValue),
                    new StringArgument("Include", "*"),
                    new BoolArgument("FollowSymlink")
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
                    new ExampleEntry
                    (
                        "Locate KeePass files in the C:\\Users\\ directory",
                        new List<string>()
                        {
                            "Get-ChildItem -Recurse -Force C:\\Users\\ -Include *.kdbx",
                            "ls -Recurse -Force C:\\Users\\ -Include *.kdbx"
                        }
                    ),
                    new ExampleEntry("List autoruns", "ls HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"),
                    new ExampleEntry("Search for files which can contain sensitive data on the C-drive", "ls -Recurse -Force C:\\ -Include *.cmd,*.bat,*.ps1,*.psm1,*.psd1"),
                    new ExampleEntry("Create directory listing of SYSVOL", "ls -Recurse -FollowSymlinks \\\\DC1\\SYSVOL"),
                    new ExampleEntry("Directory listing using LiteralPath", "Get-ChildItem -Recurse -LiteralPath \\\\?\\C:\\SomeVeryLongPath\\ -Include *.pem")
                };
            }
        }
    }
}
