using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class ConvertFromSddlStringCommand : PSCommand
    {
        public ConvertFromSddlStringCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect the (optional) ComputerName, Username and Password parameters and Verbose and WhatIf flags
            base.Execute();

            // Obtain cmdlet parameters
            string sddlString = _arguments.Get<StringArgument>("Sddl").Value;
            string sddlTypeString = _arguments.Get<StringArgument>("Type").Value;
            Type sddlType = GetSddlType(sddlTypeString);

            // Create object
            bool isDs = sddlType == typeof(ActiveDirectoryRights);
            CommonSecurityDescriptor securityDescriptor = new CommonSecurityDescriptor(false, isDs, sddlString);
            //RawSecurityDescriptor securityDescriptor = new RawSecurityDescriptor(sddlString);

            // Owner
            string ownerAccountString = string.Empty;
            if (securityDescriptor.Owner != null)
            {
                IdentityReference ownerSid = securityDescriptor.Owner;
                try
                {
                    NTAccount ownerAccount = (NTAccount)ownerSid.Translate(typeof(NTAccount));
                    ownerAccountString = ownerAccount.ToString();
                }
                catch(IdentityNotMappedException)
                {
                    ownerAccountString = ownerSid.ToString();
                }
            }

            // Group
            string groupAccountString = string.Empty;
            if (securityDescriptor.Group != null)
            {
                IdentityReference groupSid = securityDescriptor.Group;
                try
                {
                    NTAccount groupAccount = (NTAccount)groupSid.Translate(typeof(NTAccount));
                    groupAccountString = groupAccount.ToString();
                }
                catch(IdentityNotMappedException)
                {
                    groupAccountString = groupSid.ToString();
                }
            }

            // DiscretionaryAcl (Permissions)
            List<string> discretionaryAclStrings = new List<string>();
            if (securityDescriptor.DiscretionaryAcl != null)
            {
                foreach (object ace in securityDescriptor.DiscretionaryAcl)
                {
                    // Common ACE
                    if (ace is CommonAce commonAce)
                    {
                        string aceAccountName;
                        try
                        {
                            NTAccount aceAccount = (NTAccount)commonAce.SecurityIdentifier.Translate(typeof(NTAccount));
                            aceAccountName = aceAccount.Value;
                        }
                        catch(IdentityNotMappedException)
                        {
                            aceAccountName = commonAce.SecurityIdentifier.ToString();
                        }
                        object rights = Enum.ToObject(sddlType, commonAce.AccessMask);
                        discretionaryAclStrings.Add($"{aceAccountName}: {commonAce.AceType} ({rights.ToString().Replace(" | ", ", ")})");
                    }
                    // Object ACE
                    else if (ace is ObjectAce objectAce)
                    {
                        // Attempt to resolve SID to account name
                        string aceAccountName;
                        try
                        {
                            NTAccount aceAccount = (NTAccount)objectAce.SecurityIdentifier.Translate(typeof(NTAccount));
                            aceAccountName = aceAccount.Value;
                        } catch(IdentityNotMappedException)
                        {
                            aceAccountName = objectAce.SecurityIdentifier.ToString();
                        }
                        object rights = Enum.ToObject(sddlType, objectAce.AccessMask);
                        discretionaryAclStrings.Add($"{aceAccountName}: {objectAce.AceType} ({rights.ToString().Replace(" | ", ", ")})");
                    }
                }
            }

            // SystemAcl (Auditing and Logging)
            List<string> systemAclStrings = new List<string>();
            if (securityDescriptor.SystemAcl != null)
            {
                // SystemAcl (Auditing and Logging)
                if (securityDescriptor.SystemAcl != null)
                {
                    foreach (object ace in securityDescriptor.SystemAcl)
                    {
                        // Common ACE
                        if (ace is CommonAce commonAce)
                        {
                            // Attempt to resolve SID to account name
                            string aceAccountName;
                            try
                            {
                                NTAccount aceAccount = (NTAccount)commonAce.SecurityIdentifier.Translate(typeof(NTAccount));
                                aceAccountName = aceAccount.Value;
                            }
                            catch(IdentityNotMappedException)
                            {
                                aceAccountName = commonAce.SecurityIdentifier.ToString();
                            }
                            ActiveDirectoryRights rights = (ActiveDirectoryRights)commonAce.AccessMask;
                            systemAclStrings.Add($"{aceAccountName}: {commonAce.AceType} ({rights.ToString().Replace(" | ", ", ")})");
                        }
                        // Object ACE
                        else if (ace is ObjectAce objectAce)
                        {
                            // Attempt to resolve SID to account name
                            string aceAccountName;
                            try
                            {
                                NTAccount aceAccount = (NTAccount)objectAce.SecurityIdentifier.Translate(typeof(NTAccount));
                                aceAccountName = aceAccount.Value;
                            }
                            catch (IdentityNotMappedException)
                            {
                                aceAccountName = objectAce.SecurityIdentifier.ToString();
                            }
                            ActiveDirectoryRights rights = (ActiveDirectoryRights)objectAce.AccessMask;
                            systemAclStrings.Add($"{aceAccountName}: {objectAce.AceType} ({rights.ToString().Replace(" | ", ", ")})");
                        }
                    }
                }
            }

            _results.Add(
                new ResultRecord()
                {
                    { "Owner", ownerAccountString },
                    { "Group", groupAccountString },
                    { "DiscretionaryAcl", string.Join("\n", discretionaryAclStrings) },
                    { "SystemAcl", string.Join("\n", systemAclStrings) },
                    //{ "RawDescriptor", sddlString }
                }
            );

            // Return results
            return _results;
        }

        private readonly string[] validValues = { "FileSystemRights", "RegistryRights", "ActiveDirectoryRights", "MutexRights", "SemaphoreRights", "CryptoKeyRights", "EventWaitHandleRights" };
        private Type GetSddlType(string sddlTypeString)
        {
            string sddlValidType = null;
            foreach (string validValue in validValues)
            {
                if (sddlTypeString.ToLowerInvariant() == validValue.ToLowerInvariant())
                {
                    sddlValidType = validValue;
                    break;
                }
            }

            // Check if type is found
            if (string.IsNullOrEmpty(sddlValidType))
                throw new NoPowerShellException($"Cannot validate argument on parameter 'Type'. The argument \"{sddlTypeString}\" does not belong to the set \"{string.Join(",", validValues)}\".");

            // Initiate type
            Type sddlType = null;
            if (sddlValidType == "ActiveDirectoryRights")
                sddlType = typeof(System.DirectoryServices.ActiveDirectoryRights);
            else
                sddlType = Type.GetType($"System.Security.AccessControl.{sddlValidType}");

            // Throw error if not possible to initialize
            if (sddlType == null)
                throw new NoPowerShellException($"Type \"{sddlValidType}\" could not be initialized");

            return sddlType;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "ConvertFrom-SddlString" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Sddl"),
                    new StringArgument("Type", "FileSystemRights", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Converts a SDDL string to a custom object."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Convert Active Directory SDDL (nTSecurityDescriptor) to readable format' command", "ConvertFrom-SddlString \"D:(A;;CR;;;S-1-5-21-2137271609-6538894-3613171323-1144)\" -Type ActiveDirectoryRights"),
                    new ExampleEntry
                    (
                        "Convert filesystem SDDL to readable format",
                        new List<string>()
                        {
                            "ConvertFrom-SddlString \"O:BAG:BAD:(A;;FA;;;BA)(A;;0x1200a9;;;SY)\"",
                            "ConvertFrom-SddlString \"O:BAG:BAD:(A;;FA;;;BA)(A;;0x1200a9;;;SY)\" -Type FileSystemrights"
                        }
                    )
                };
            }
        }
    }
}
