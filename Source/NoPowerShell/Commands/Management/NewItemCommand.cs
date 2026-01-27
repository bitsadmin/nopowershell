using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class NewItemCommand : PSCommand
    {
        public NewItemCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string pathArgument = _arguments.Get<StringArgument>("Path").Value;
            string nameArgument = _arguments.Get<StringArgument>("Name").Value;
            string typeArgument = _arguments.Get<StringArgument>("Type").Value;
            string valueArgument = _arguments.Get<StringArgument>("Value").Value;
            string sizeArgument = _arguments.Get<StringArgument>("Size").Value;

            ItemCreationType itemType = ParseItemType(typeArgument);
            string targetPath = BuildTargetPath(pathArgument, nameArgument);

            if (itemType == ItemCreationType.Directory && !string.IsNullOrEmpty(sizeArgument))
                throw new NoPowerShellException("The Size parameter cannot be used when creating directories.");

            if (itemType == ItemCreationType.Directory && !string.IsNullOrEmpty(valueArgument))
                Program.WriteWarning("Value parameter is ignored when creating directories.");

            if ((itemType == ItemCreationType.RandomFile || itemType == ItemCreationType.EmptyFile) && string.IsNullOrEmpty(sizeArgument))
                throw new NoPowerShellException("The Size parameter must be specified when creating RandomFile or EmptyFile items.");

            if ((itemType == ItemCreationType.RandomFile || itemType == ItemCreationType.EmptyFile) && !string.IsNullOrEmpty(valueArgument))
                Program.WriteWarning("Value parameter is ignored when creating sized files.");

            ResultRecord createdItemRecord;

            switch (itemType)
            {
                case ItemCreationType.Directory:
                    createdItemRecord = CreateDirectory(targetPath);
                    break;

                case ItemCreationType.RandomFile:
                case ItemCreationType.EmptyFile:
                    long requestedSize = ParseSize(sizeArgument);
                    createdItemRecord = CreateSizedFile(targetPath, requestedSize, itemType == ItemCreationType.RandomFile);
                    break;

                default:
                    createdItemRecord = CreateStandardFile(targetPath, valueArgument);
                    break;
            }

            _results.Add(createdItemRecord);

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "New-Item",
            "ni",
            "md",
            "mkdir"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Path", "."),
            new StringArgument("Name", true),
            new StringArgument("Type", "File"),
            new StringArgument("Value", true),
            new StringArgument("Size", true)
        };

        public static new string Synopsis => "Creates filesystem items (files, directories, and sized RandomFile/EmptyFile variants) without relying on System.Management.Automation.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Create a directory", "New-Item -Name MyNewDir -Type Directory"),
            new ExampleEntry("Create a file with custom contents", "New-Item -Name MyFile.txt -Type File -Value \"Contents of file\""),
            new ExampleEntry
            (
                "Create files of a specific size",
                new List<string>()
                {
                    "New-Item -Name file.bin -Type RandomFile -Size 20M",
                    "New-Item -Path C:\\Tmp\\empty.bin -Type EmptyFile -Size 1.5G"
                }
            )
        };

        private static ResultRecord CreateDirectory(string targetPath)
        {
            EnsureItemDoesNotExist(targetPath);
            Directory.CreateDirectory(targetPath);

            DirectoryInfo directoryInfo = new DirectoryInfo(targetPath);
            directoryInfo.Refresh();

            return BuildResultRecord(directoryInfo);
        }

        private static ResultRecord CreateStandardFile(string targetPath, string value)
        {
            EnsureParentDirectoryExists(targetPath);
            EnsureItemDoesNotExist(targetPath);

            if (value != null)
            {
                File.WriteAllText(targetPath, value);
            }
            else
            {
                using (FileStream fs = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                }
            }

            FileInfo fileInfo = new FileInfo(targetPath);
            fileInfo.Refresh();

            return BuildResultRecord(fileInfo);
        }

        private static ResultRecord CreateSizedFile(string targetPath, long sizeInBytes, bool randomContent)
        {
            if (sizeInBytes < 0)
                throw new NoPowerShellException("Size must be zero or greater.");

            EnsureParentDirectoryExists(targetPath);
            EnsureItemDoesNotExist(targetPath);

            using (FileStream fs = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (randomContent)
                {
                    WriteRandomContent(fs, sizeInBytes);
                }
                else
                {
                    fs.SetLength(sizeInBytes);
                }
            }

            FileInfo fileInfo = new FileInfo(targetPath);
            fileInfo.Refresh();

            return BuildResultRecord(fileInfo);
        }

        private static void WriteRandomContent(Stream destination, long bytesToWrite)
        {
            if (bytesToWrite == 0)
                return;

            byte[] buffer = new byte[Math.Min(bytesToWrite, 1024 * 1024)];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                long remaining = bytesToWrite;

                while (remaining > 0)
                {
                    int currentChunk = remaining > buffer.Length ? buffer.Length : (int)remaining;
                    rng.GetBytes(buffer);
                    destination.Write(buffer, 0, currentChunk);
                    remaining -= currentChunk;
                }
            }
        }

        private static ItemCreationType ParseItemType(string typeArgument)
        {
            string normalized = string.IsNullOrEmpty(typeArgument)
                ? "File"
                : typeArgument.Trim();

            switch (normalized.ToUpperInvariant())
            {
                case "FILE":
                    return ItemCreationType.File;

                case "DIRECTORY":
                case "DIR":
                case "FOLDER":
                    return ItemCreationType.Directory;

                case "RANDOMFILE":
                    return ItemCreationType.RandomFile;

                case "EMPTYFILE":
                    return ItemCreationType.EmptyFile;

                default:
                    throw new NoPowerShellException("Unsupported Type value '{0}'. Valid values: Directory, File, RandomFile, EmptyFile.", typeArgument);
            }
        }

        private static string BuildTargetPath(string pathArgument, string nameArgument)
        {
            string effectivePath = string.IsNullOrWhiteSpace(pathArgument) ? "." : pathArgument;
            string expanded = Environment.ExpandEnvironmentVariables(effectivePath);

            if (!string.IsNullOrEmpty(nameArgument))
                expanded = Path.Combine(expanded, nameArgument);
            else if (IsDefaultPathValue(pathArgument))
                throw new NoPowerShellException("Specify -Name or provide the full destination via -Path.");

            return Path.GetFullPath(expanded);
        }

        private static bool IsDefaultPathValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            string trimmed = value.Trim();
            return trimmed == "." || trimmed == string.Empty;
        }

        private static void EnsureItemDoesNotExist(string targetPath)
        {
            if (File.Exists(targetPath) || Directory.Exists(targetPath))
                throw new NoPowerShellException("An item with the specified name already exists: {0}", targetPath);
        }

        private static void EnsureParentDirectoryExists(string targetPath)
        {
            string parent = Path.GetDirectoryName(targetPath);

            if (string.IsNullOrEmpty(parent))
                parent = Directory.GetCurrentDirectory();

            if (!Directory.Exists(parent))
                throw new NoPowerShellException("Cannot find path '{0}' because it does not exist.", parent);
        }

        private static ResultRecord BuildResultRecord(FileSystemInfo info)
        {
            ResultRecord record = new ResultRecord()
            {
                { "Mode", GetModeFlags(info) },
                { "LastWriteTime", info.LastWriteTime.ToFormattedString() },
                { "Length", info is FileInfo fileInfo ? fileInfo.Length.ToString() : string.Empty },
                { "Name", info.Name }
            };

            return record;
        }

        private static string GetModeFlags(FileSystemInfo info)
        {
            StringBuilder sb = new StringBuilder(6);

            sb.Append((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory ? "d" : "-");
            sb.Append((info.Attributes & FileAttributes.Archive) == FileAttributes.Archive ? "a" : "-");
            sb.Append((info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ? "r" : "-");
            sb.Append((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ? "h" : "-");
            sb.Append((info.Attributes & FileAttributes.System) == FileAttributes.System ? "s" : "-");
            sb.Append((info.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint ? "l" : "-");

            return sb.ToString();
        }

        private static long ParseSize(string sizeArgument)
        {
            if (string.IsNullOrWhiteSpace(sizeArgument))
                throw new NoPowerShellException("Size parameter cannot be empty.");

            string trimmed = sizeArgument.Trim();
            decimal multiplier = 1m;

            char suffix = trimmed[trimmed.Length - 1];
            if (char.IsLetter(suffix))
            {
                switch (char.ToUpperInvariant(suffix))
                {
                    case 'K':
                        multiplier = 1024m;
                        break;
                    case 'M':
                        multiplier = 1024m * 1024m;
                        break;
                    case 'G':
                        multiplier = 1024m * 1024m * 1024m;
                        break;
                    default:
                        throw new NoPowerShellException("Unsupported size suffix '{0}'. Use K, M or G.", suffix);
                }

                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
                throw new NoPowerShellException("Invalid size value '{0}'.", sizeArgument);

            if (value < 0)
                throw new NoPowerShellException("Size must be zero or greater.");

            decimal totalBytes = value * multiplier;
            if (totalBytes > long.MaxValue)
                throw new NoPowerShellException("Requested size is too large.");

            decimal rounded = decimal.Round(totalBytes, MidpointRounding.AwayFromZero);
            return decimal.ToInt64(rounded);
        }

        private enum ItemCreationType
        {
            File,
            Directory,
            RandomFile,
            EmptyFile
        }
    }
}
