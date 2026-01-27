using Microsoft.Win32;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
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
        public GetChildItemCommand(string[] userArguments) : base(userArguments)
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
                _results = BrowseRegistry(root, path, recurse, depth);
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
                if (!useLiteralPath)
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

        private static CommandResult BrowseRegistry(RegistryHive root, string path, bool recurse, int depth)
        {
            CommandResult results = new CommandResult();
            string displayPath = BuildRegistryDisplayPath(root, path);

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64))
            {
                if (string.IsNullOrEmpty(path))
                {
                    EnumerateRegistrySubKeys(baseKey, results, recurse, depth);
                }
                else
                {
                    try
                    {
                        using (RegistryKey key = baseKey.OpenSubKey(path))
                        {
                            if (key == null)
                                throw new ItemNotFoundException(displayPath);

                            EnumerateRegistrySubKeys(key, results, recurse, depth);
                        }
                    }
                    catch (SecurityException)
                    {
                        Program.WriteError($"Access to the path '{baseKey.Name}\\{path}' is denied.");
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
                foreach (DictionaryEntry variable in variables)
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

        private static void EnumerateRegistrySubKeys(RegistryKey key, CommandResult results, bool recurse, int depth)
        {
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                ResultRecord currentKey = new ResultRecord()
                {
                    { "Name", recurse ? BuildRegistryFullName(key.Name, subkeyName) : subkeyName }
                };

                results.Add(currentKey);

                if (recurse && depth > 0)
                {
                    try
                    {
                        using (RegistryKey childKey = key.OpenSubKey(subkeyName))
                        {
                            if (childKey == null)
                                continue;

                            EnumerateRegistrySubKeys(childKey, results, true, depth - 1);
                        }
                    }
                    catch (SecurityException)
                    {
                        Program.WriteError($"Access to the path '{key.Name}\\{subkeyName}' is denied.");
                    }
                }
            }
        }

        private static string BuildRegistryFullName(string parentKeyName, string childName)
        {
            if (string.IsNullOrEmpty(parentKeyName))
                return childName;

            if (parentKeyName.EndsWith("\\", StringComparison.Ordinal))
                return parentKeyName + childName;

            return parentKeyName + "\\" + childName;
        }

        private static string BuildRegistryDisplayPath(RegistryHive root, string path)
        {
            string hiveName = GetRegistryHiveName(root);
            if (string.IsNullOrEmpty(path))
                return $"{hiveName}:\\";

            return $"{hiveName}:\\{path}";
        }

        private static string GetRegistryHiveName(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.LocalMachine:
                    return "HKLM";
                case RegistryHive.CurrentUser:
                    return "HKCU";
                case RegistryHive.ClassesRoot:
                    return "HKCR";
                case RegistryHive.Users:
                    return "HKU";
                default:
                    return hive.ToString();
            }
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

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-ChildItem",
            "gci",
            "ls",
            "dir"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Path", "."),
            new StringArgument("LiteralPath", true),
            new BoolArgument("Force") ,
            new BoolArgument("Recurse"),
            new IntegerArgument("Depth", int.MaxValue),
            new StringArgument("Include", "*"),
            new BoolArgument("FollowSymlink")
        };

        public static new string Synopsis => "Gets the files and folders in a file system drive.";

        public static new ExampleEntries Examples => new ExampleEntries()
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
