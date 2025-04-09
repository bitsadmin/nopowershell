using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Security
{
    public class GetAclCommand : PSCommand
    {
        public GetAclCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect the (optional) ComputerName, Username and Password parameters and Verbose and WhatIf flags
            base.Execute();

            // Obtain cmdlet parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string server = _arguments.Get<StringArgument>("Server").Value;

            // Variables for output
            string fullPath = null;
            string ownerAccountString = null;
            string groupAccountString = null;
            string sddlString = null;
            List<string> accessRuleStrings = new List<string>();
            List<string> auditRuleStrings = new List<string>();

            // Registry
            string registryPattern = @"^(HKLM|HKCU|HKCR|HKU):.*$";
            Regex registryRegex = new Regex(registryPattern);
            if (registryRegex.IsMatch(path))
            {
                throw new NoPowerShellException("registry not implemented yet");
            }
            // ActiveDirectory object
            if (path.StartsWith("AD:", StringComparison.InvariantCultureIgnoreCase))
            {
                string distinguishedName = path.Replace("AD:\\", "");

                // Perform query
                CommandResult adObject = LDAPHelper.QueryLDAP(
                    null,
                    $"(distinguishedName={distinguishedName})",
                    new List<string>() { "distinguishedName", "nTSecurityDescriptor" },
                    server,
                    username,
                    password
                );

                // Throw execption if no results
                if (adObject == null)
                {
                    throw new NoPowerShellException($"Cannot find path \"{path}\" because it does not exist.");
                }

                // Obtain values
                fullPath = adObject[0]["distinguishedName"];
                sddlString = adObject[0]["nTSecurityDescriptor"];
                CommonSecurityDescriptor securityDescriptor = new CommonSecurityDescriptor(false, true, sddlString);

                // Owner
                if (securityDescriptor.Owner != null)
                    ownerAccountString = GetAccountName(securityDescriptor.Owner);

                // Group
                if (securityDescriptor.Group != null)
                    groupAccountString = GetAccountName(securityDescriptor.Group);

                // DACL
                if (securityDescriptor.DiscretionaryAcl != null)
                    accessRuleStrings = ProcessAces(securityDescriptor.DiscretionaryAcl);

                // System ACL
                if (securityDescriptor.SystemAcl != null)
                    auditRuleStrings = ProcessAces(securityDescriptor.SystemAcl);
            }
            // Filesystem
            else
            {
                AuthorizationRuleCollection accessRules = null, auditRules = null;
                NTAccount ownerAccount = null, groupAccount = null;

                // Directory
                if (Directory.Exists(path))
                {
                    // Get directory ACL
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
                    sddlString = directorySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                    fullPath = directoryInfo.FullName;

                    // Owner
                    ownerAccount = (NTAccount)directorySecurity.GetOwner(typeof(NTAccount));

                    // Access/Audit rules
                    accessRules = directorySecurity.GetAccessRules(true, true, typeof(NTAccount));
                    auditRules = directorySecurity.GetAuditRules(true, true, typeof(NTAccount));
                }
                // File
                else if (File.Exists(path))
                {
                    // Get file ACL
                    FileInfo fileInfo = new FileInfo(path);
                    FileSecurity fileSecurity = fileInfo.GetAccessControl();
                    sddlString = fileSecurity.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                    fullPath = fileInfo.FullName;

                    // Owner
                    ownerAccount = (NTAccount)fileSecurity.GetOwner(typeof(NTAccount));

                    // Group
                    groupAccount = (NTAccount)fileSecurity.GetGroup(typeof(NTAccount));

                    // Access/Audit rules
                    accessRules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));
                    auditRules = fileSecurity.GetAuditRules(true, true, typeof(NTAccount));
                }

                // Owner
                if (ownerAccount != null)
                    ownerAccountString = ownerAccount.ToString();

                // Group
                if (groupAccount != null)
                    groupAccountString = groupAccount.ToString();

                // Parse access rules
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    accessRuleStrings.Add($"{rule.IdentityReference}: {rule.AccessControlType} ({rule.FileSystemRights.ToString().Replace(" | ", ", ")})");
                }

                // Parse audit rules
                foreach (FileSystemAuditRule rule in auditRules)
                {
                    auditRuleStrings.Add($"{rule.IdentityReference}: ({rule.AuditFlags.ToString().Replace(" | ", ", ")})");
                }
            }

            _results.Add(
                new ResultRecord()
                {
                    { "Path", fullPath },
                    { "Owner", ownerAccountString },
                    { "Group", groupAccountString },
                    { "Access", string.Join("\n", accessRuleStrings) },
                    { "Audit", string.Join("\n", auditRuleStrings) },
                    { "Sddl", sddlString }
                }
            );

            // Return results
            return _results;
        }

        private static List<string> ProcessAces(CommonAcl aces)
        {
            List<string> accessRuleStrings = new List<string>();

            foreach (object ace in aces)
            {
                string aceAccountName;
                ActiveDirectoryRights rights;

                // Check for Common ACE
                if (ace is CommonAce commonAce)
                {
                    aceAccountName = GetAccountName(commonAce.SecurityIdentifier);
                    rights = (ActiveDirectoryRights)commonAce.AccessMask;
                    accessRuleStrings.Add($"{aceAccountName}: {commonAce.AceType} ({rights.ToString().Replace(" | ", ", ")})");
                }
                // Check for Object ACE
                else if (ace is ObjectAce objectAce)
                {
                    aceAccountName = GetAccountName(objectAce.SecurityIdentifier);
                    rights = (ActiveDirectoryRights)objectAce.AccessMask;
                    accessRuleStrings.Add($"{aceAccountName}: {objectAce.AceType} ({rights.ToString().Replace(" | ", ", ")})");
                }
            }

            return accessRuleStrings;
        }

        private static string GetAccountName(SecurityIdentifier securityIdentifier)
        {
            string accountName;
            try
            {
                NTAccount account = (NTAccount)securityIdentifier.Translate(typeof(NTAccount));
                accountName = account.Value;
            }
            catch (IdentityNotMappedException)
            {
                accountName = securityIdentifier.ToString();
            }
            return accountName;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Acl" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("Server", true) // Just used in case Get-Acl is used on an AD object from outside of the domain
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the security descriptor for a resource, such as a file or registry key."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "List ACLs of file",
                        new List<string>()
                        {
                            "Get-Acl C:\\Windows\\explorer.exe",
                            "Get-Acl -Path C:\\Windows\\explorer.exe"
                        }
                    ),
                    new ExampleEntry("List ACLs of directory", "Get-Acl C:\\Windows"),
                    new ExampleEntry("List ACLs of AD Object", "Get-Acl \"AD:\\CN=User One,CN=Users,DC=ad,DC=bitsadmin,DC=com\"")
                };
            }
        }
    }
}
