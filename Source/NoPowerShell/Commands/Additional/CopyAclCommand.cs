using NoPowerShell.Arguments;
using NoPowerShell.Commands.Security;
using NoPowerShell.HelperClasses;
using System;
using System.IO;
using System.Security.AccessControl;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class CopyAclCommand : PSCommand
    {
        public CopyAclCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string destination = _arguments.Get<StringArgument>("Destination").Value;
            bool recurse = _arguments.Get<BoolArgument>("Recurse").Value;

            // Check if path exists
            if (!(Directory.Exists(path) || File.Exists(path)))
                throw new ArgumentException(path + " does not exist.");
            if (!(Directory.Exists(destination) || File.Exists(destination)))
                throw new ArgumentException(destination + " does not exist.");

            // Check if path and destination are the same type
            if (!(Directory.Exists(path) == Directory.Exists(destination)))
                throw new ArgumentException("Path and destination must be the same type (file or directory).");

            // Display ACL of source path
            Program.WriteVerbose("Source ACL:");
            GetAclCommand sourceAcl = new GetAclCommand(new string[] { path });
            CommandResult sourceAclResult = sourceAcl.Execute(null);
            Console.Write(ResultPrinter.FormatList(sourceAclResult));

            // Display ACL of destination path
            Program.WriteVerbose("Current destination ACL:");
            GetAclCommand destinationAcl = new GetAclCommand(new string[] { destination });
            CommandResult destinationAclResult = destinationAcl.Execute(null);
            Console.Write(ResultPrinter.FormatList(destinationAclResult));

            // Copy access rules
            if (recurse)
                Program.WriteVerbose("Recursively copying ACLs from {0} to {1}", path, destination);
            else
                Program.WriteVerbose("Copying ACL from {0} to {1}", path, destination);
            CopyAccessRules(path, destination, recurse);
            Console.WriteLine();

            // Display updated ACL of destination path
            Program.WriteVerbose("Updated destination ACL:");
            destinationAcl = new GetAclCommand(new string[] { destination });
            destinationAclResult = destinationAcl.Execute(null);
            Console.Write(ResultPrinter.FormatList(destinationAclResult));

            // Return results
            return _results;
        }

        static void CopyAccessRules(string sourcePath, string destinationPath, bool recursive)
        {
            if (Directory.Exists(sourcePath))
            {
                // Copying directory access rules
                DirectorySecurity directorySecurity = Directory.GetAccessControl(sourcePath);
                directorySecurity.SetAccessRuleProtection(true, true);
                Program.WriteVerbose("Updating ACL on folder {0}", destinationPath);
                Directory.SetAccessControl(destinationPath, directorySecurity);

                if (recursive)
                {
                    // Recurse into subdirectories
                    foreach (var directory in Directory.GetDirectories(destinationPath))
                    {
                        string destinationDir = Path.Combine(destinationPath, Path.GetFileName(directory));
                        CopyAccessRules(directory, destinationDir, true);
                    }

                    // Update access rules for files in the directory
                    foreach (var file in Directory.GetFiles(destinationPath))
                    {
                        string destinationFile = Path.Combine(destinationPath, Path.GetFileName(file));
                        CopyAccessRules(file, destinationFile, false);
                    }
                }
            }
            else if (File.Exists(sourcePath))
            {
                // Copying file access rules
                FileSecurity fileSecurity = File.GetAccessControl(sourcePath);
                fileSecurity.SetAccessRuleProtection(true, true);
                Program.WriteVerbose("Updating ACL on file {0}", destinationPath);
                File.SetAccessControl(destinationPath, fileSecurity);
            }
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Copy-Acl" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("Destination"),
                    new BoolArgument("Recurse")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Copies access ACLs from source to destination file/directory"; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Copy ACL from file to another file", "Copy-Acl C:\\Data\\file.txt C:\\Data\\destination.txt"),
                    new ExampleEntry("Recursively copy ACLs from source to destination folder", "Copy-Acl -Recurse -Path C:\\SourceDir -Destination C:\\DestinationDir")
                };
            }
        }
    }
}
