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
    public class GetPSDriveCommand : PSCommand
    {
        public GetPSDriveCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Enumerate harddrives
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", drive.Name[0].ToString() },
                        { "Used (GB)", drive.IsReady ? ((drive.TotalSize - drive.TotalFreeSpace) / Math.Pow(1024,3)).ToString("0.00") : "" },
                        { "Free (GB)", drive.IsReady ? (drive.TotalFreeSpace / Math.Pow(1024,3)).ToString("0.00") : "" },
                        { "Provider", "FileSystem" },
                        { "Root", drive.Name }
                    }
                );
            }

            // Complement with environment and registry
            _results.AddRange(
                new List<ResultRecord>()
                {
                    new ResultRecord()
                    {
                        { "Name", "Env" },
                        { "Used (GB)", "" },
                        { "Free (GB)", "" },
                        { "Provider", "Environment" },
                        { "Root", "" }
                    },
                    new ResultRecord()
                    {
                        { "Name", "HKCU" },
                        { "Used (GB)", "" },
                        { "Free (GB)", "" },
                        { "Provider", "Registry" },
                        { "Root", "HKEY_CURRENT_USER" }
                    },
                    new ResultRecord()
                    {
                        { "Name", "HKLM" },
                        { "Used (GB)", "" },
                        { "Free (GB)", "" },
                        { "Provider", "Registry" },
                        { "Root", "HKEY_LOCAL_MACHINE" }
                    }
                }
            );

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-PSDrive", "gdr" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets drives in the current session."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "List drives",
                        new List<string>()
                        {
                            "Get-PSDrive",
                            "gdr"
                        }
                    )
                };
            }
        }
    }
}
