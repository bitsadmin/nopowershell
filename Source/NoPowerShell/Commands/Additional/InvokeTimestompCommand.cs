using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class InvokeTimestompCommand : PSCommand
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

        public InvokeTimestompCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string targetPath = _arguments.Get<StringArgument>("TargetPath").Value;
            string sourcePath = _arguments.Get<StringArgument>("SourcePath").Value;
            string timestampValue = _arguments.Get<StringArgument>("Timestamp").Value;
            string creationTimeValue = _arguments.Get<StringArgument>("CreationTime").Value;
            string lastAccessTimeValue = _arguments.Get<StringArgument>("LastAccessTime").Value;
            string lastWriteTimeValue = _arguments.Get<StringArgument>("LastWriteTime").Value;

            if (string.IsNullOrEmpty(targetPath))
                throw new NoPowerShellException("TargetPath must be specified.");

            TimestampSet timestamps = ResolveTimestampValues(sourcePath, timestampValue, creationTimeValue, lastAccessTimeValue, lastWriteTimeValue);
            List<string> targets = ResolveTargetFiles(targetPath);

            foreach (string file in targets)
            {
                ApplyTimestamps(file, timestamps);

                FileInfo info = new FileInfo(file);
                _results.Add(
                    new ResultRecord()
                    {
                        { "Path", info.FullName },
                        { "CreationTime", info.CreationTime.ToString(TimestampFormat, CultureInfo.InvariantCulture) },
                        { "LastAccessTime", info.LastAccessTime.ToString(TimestampFormat, CultureInfo.InvariantCulture) },
                        { "LastWriteTime", info.LastWriteTime.ToString(TimestampFormat, CultureInfo.InvariantCulture) }
                    }
                );
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Invoke-Timestomp"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("TargetPath"),
            new StringArgument("SourcePath", true),
            new StringArgument("Timestamp", true),
            new StringArgument("CreationTime", true),
            new StringArgument("LastAccessTime", true),
            new StringArgument("LastWriteTime", true)
        };

        public static new string Synopsis => "Apply custom timestamps to one or more files.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Copy timestamps from an existing binary", @"Invoke-Timestomp -TargetPath C:\Tools\payload.exe -SourcePath C:\Windows\System32\notepad.exe"),
            new ExampleEntry("Stamp every DLL in a folder with the same timestamp", @"Invoke-Timestomp -TargetPath ""C:\Temp\*.dll"" -Timestamp ""2024-01-01 09:00:00"""),
            new ExampleEntry
            (
                "Set each timestamp explicitly",
                @"Invoke-Timestomp -TargetPath C:\Tools\agent.dll -CreationTime ""2023-12-24 12:00:00"" -LastAccessTime ""2023-12-25 12:00:00"" -LastWriteTime ""2023-12-26 12:00:00"""
            )
        };

        private static TimestampSet ResolveTimestampValues(string sourcePath, string timestampValue, string creationTimeValue, string lastAccessTimeValue, string lastWriteTimeValue)
        {
            bool hasSource = !string.IsNullOrEmpty(sourcePath);
            bool hasUnified = !string.IsNullOrEmpty(timestampValue);
            bool hasIndividual = !string.IsNullOrEmpty(creationTimeValue) || !string.IsNullOrEmpty(lastAccessTimeValue) || !string.IsNullOrEmpty(lastWriteTimeValue);
            int modeCount = (hasSource ? 1 : 0) + (hasUnified ? 1 : 0) + (hasIndividual ? 1 : 0);

            if (modeCount == 0)
                throw new NoPowerShellException("Specify -SourcePath, -Timestamp or all individual timestamp parameters.");
            if (modeCount > 1)
                throw new NoPowerShellException("Specify only one timestamp source: -SourcePath, -Timestamp or the individual timestamp parameters.");

            if (hasSource)
            {
                if (!File.Exists(sourcePath))
                    throw new NoPowerShellException(string.Format("Source path '{0}' does not exist.", sourcePath));

                FileInfo info = new FileInfo(sourcePath);
                return new TimestampSet(info.CreationTime, info.LastAccessTime, info.LastWriteTime);
            }

            if (hasUnified)
            {
                DateTime parsed = ParseTimestamp(timestampValue);
                return new TimestampSet(parsed, parsed, parsed);
            }

            if (string.IsNullOrEmpty(creationTimeValue) || string.IsNullOrEmpty(lastAccessTimeValue) || string.IsNullOrEmpty(lastWriteTimeValue))
                throw new NoPowerShellException("When using individual timestamps, supply values for -CreationTime, -LastAccessTime and -LastWriteTime.");

            return new TimestampSet(
                ParseTimestamp(creationTimeValue),
                ParseTimestamp(lastAccessTimeValue),
                ParseTimestamp(lastWriteTimeValue)
            );
        }

        private static List<string> ResolveTargetFiles(string targetPath)
        {
            List<string> resolvedTargets = new List<string>();
            bool containsWildcard = targetPath.IndexOfAny(new char[] { '*', '?' }) >= 0;

            if (!containsWildcard)
            {
                if (!File.Exists(targetPath))
                    throw new NoPowerShellException(string.Format("Target path '{0}' does not exist.", targetPath));

                resolvedTargets.Add(Path.GetFullPath(targetPath));
                return resolvedTargets;
            }

            string directory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(directory))
                directory = Directory.GetCurrentDirectory();

            string searchPattern = Path.GetFileName(targetPath);
            if (string.IsNullOrEmpty(searchPattern))
                searchPattern = "*";

            if (!Directory.Exists(directory))
                throw new NoPowerShellException(string.Format("Directory '{0}' does not exist.", directory));

            string[] matches = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            if (matches.Length == 0)
                throw new NoPowerShellException(string.Format("No files matched target path '{0}'.", targetPath));

            foreach (string match in matches)
                resolvedTargets.Add(Path.GetFullPath(match));

            return resolvedTargets;
        }

        private static void ApplyTimestamps(string path, TimestampSet timestamps)
        {
            File.SetCreationTime(path, timestamps.CreationTime);
            File.SetLastAccessTime(path, timestamps.LastAccessTime);
            File.SetLastWriteTime(path, timestamps.LastWriteTime);
        }

        private static DateTime ParseTimestamp(string value)
        {
            try
            {
                return DateTime.ParseExact(value, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
            catch (FormatException)
            {
                throw new NoPowerShellException(
                    string.Format("Timestamp '{0}' is invalid. Use the format {1}.", value, TimestampFormat));
            }
        }

        private struct TimestampSet
        {
            public TimestampSet(DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
            {
                CreationTime = creationTime;
                LastAccessTime = lastAccessTime;
                LastWriteTime = lastWriteTime;
            }

            public DateTime CreationTime { get; }
            public DateTime LastAccessTime { get; }
            public DateTime LastWriteTime { get; }
        }
    }
}
