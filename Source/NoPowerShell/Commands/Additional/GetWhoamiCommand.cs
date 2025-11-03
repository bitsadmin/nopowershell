using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class GetWhoamiCommand : PSCommand
    {
        public GetWhoamiCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            bool showGroups = _arguments.Get<BoolArgument>("Groups").Value;
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Show groups
            if (showGroups)
            {
                IntPtr tokenHandle;
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, out tokenHandle))
                {
                    throw new NoPowerShellException("Failed to open process token.");
                }

                try
                {
                    // Get token groups (user's group memberships)
                    int tokenInfoLength;
                    GetTokenInformation(tokenHandle, TokenGroups, IntPtr.Zero, 0, out tokenInfoLength);
                    IntPtr tokenInfo = Marshal.AllocHGlobal(tokenInfoLength);

                    if (GetTokenInformation(tokenHandle, TokenGroups, tokenInfo, tokenInfoLength, out tokenInfoLength))
                    {
                        int groupCount = Marshal.ReadInt32(tokenInfo);
                        int groupsOffset = Marshal.OffsetOf(typeof(TOKEN_GROUPS), nameof(TOKEN_GROUPS.Groups)).ToInt32();
                        IntPtr groupsPtr = IntPtr.Add(tokenInfo, groupsOffset);

                        for (int i = 0; i < groupCount; i++)
                        {
                            SID_AND_ATTRIBUTES sidAttr = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(groupsPtr);
                            string attrs = GetAttributeNames(sidAttr.Attributes);
                            string groupName = string.Empty;

                            if (sidAttr.Sid != IntPtr.Zero && ConvertSidToStringSid(sidAttr.Sid, out IntPtr sidString))
                            {
                                string sidStr = Marshal.PtrToStringUni(sidString); // Unicode
                                string type = GetSidType(sidStr);

                                // Only include groups
                                if (!sidStr.StartsWith("S-1-16-"))
                                {
                                    try
                                    {
                                        var sid = new SecurityIdentifier(sidStr);
                                        NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                                        groupName = account.Value;
                                    }
                                    catch (IdentityNotMappedException)
                                    {
                                        groupName = string.Empty;
                                    }

                                    _results.Add(new ResultRecord
                                    {
                                        { "GroupName", groupName },
                                        { "GroupType", type },
                                        { "GroupSID", sidStr },
                                        { "GroupAttributes", attrs }
                                    });
                                }

                                LocalFree(sidString);
                            }

                            groupsPtr = IntPtr.Add(groupsPtr, Marshal.SizeOf(typeof(SID_AND_ATTRIBUTES)));
                        }
                    }
                    Marshal.FreeHGlobal(tokenInfo);

                    // Get integrity level
                    GetTokenInformation(tokenHandle, TokenIntegrityLevel, IntPtr.Zero, 0, out tokenInfoLength);
                    IntPtr ilInfo = Marshal.AllocHGlobal(tokenInfoLength);
                    if (GetTokenInformation(tokenHandle, TokenIntegrityLevel, ilInfo, tokenInfoLength, out tokenInfoLength))
                    {
                        SID_AND_ATTRIBUTES ilSidAttr = Marshal.PtrToStructure<SID_AND_ATTRIBUTES>(ilInfo);
                        string attrs = GetAttributeNames(ilSidAttr.Attributes);

                        if (ConvertSidToStringSid(ilSidAttr.Sid, out IntPtr sidString))
                        {
                            string sidStr = Marshal.PtrToStringAuto(sidString);
                            int rid = GetIntegrityLevelRID(ilSidAttr.Sid);
                            string level = string.Format("Mandatory Label\\{0}", GetIntegrityLevelName(rid));
                            string type = GetSidType(sidStr);
                            _results.Add(new ResultRecord
                            {
                                { "GroupName", level },
                                { "GroupType", type },
                                { "GroupSID", sidStr },
                                { "GroupAttributes", attrs }
                            });
                            Marshal.FreeHGlobal(sidString);
                        }
                    }
                    Marshal.FreeHGlobal(ilInfo);
                }
                finally
                {
                    CloseHandle(tokenHandle);
                }
            }
            // Just show domain\username
            else
            {
                string[] DomainUser = identity.Name.Split('\\');

                _results.Add(
                    new ResultRecord()
                    {
                        { "Domain", DomainUser[0] },
                        { "User", DomainUser[1] },
                        { "SID", identity.User.Value }
                    }
                );
            }

            return _results;
        }

        static string GetAttributeNames(int attr)
        {
            string result = "";
            if ((attr & SE_GROUP_MANDATORY) != 0) result += "Mandatory ";
            if ((attr & SE_GROUP_ENABLED_BY_DEFAULT) != 0) result += "EnabledByDefault ";
            if ((attr & SE_GROUP_ENABLED) != 0) result += "Enabled ";
            if ((attr & SE_GROUP_OWNER) != 0) result += "Owner ";
            if ((attr & SE_GROUP_USE_FOR_DENY_ONLY) != 0) result += "DenyOnly ";
            if ((attr & SE_GROUP_INTEGRITY) != 0) result += "Integrity ";
            if ((attr & SE_GROUP_INTEGRITY_ENABLED) != 0) result += "IntegrityEnabled ";
            if ((attr & SE_GROUP_LOGON_ID) != 0) result += "LogonId ";
            if ((attr & SE_GROUP_RESOURCE) != 0) result += "LocalGroup ";
            if (string.IsNullOrEmpty(result)) result = "-";
            return result.Trim();
        }

        static int GetIntegrityLevelRID(IntPtr sid)
        {
            IntPtr subAuthCountPtr = IntPtr.Add(sid, Marshal.ReadByte(IntPtr.Add(sid, 1)) * 4 + 8);
            int subAuthCount = Marshal.ReadByte(IntPtr.Add(sid, 1));
            IntPtr ridPtr = IntPtr.Add(sid, 8 + (subAuthCount - 1) * 4);
            return Marshal.ReadInt32(ridPtr);
        }

        static string GetIntegrityLevelName(int rid)
        {
            if (rid == 0x00001000)
                return "Low Mandatory Level";
            else if (rid == 0x00002000)
                return "Medium Mandatory Level";
            else if (rid == 0x00003000)
                return "High Mandatory Level";
            else if (rid == 0x00004000)
                return "System Mandatory Level";
            else
                return $"Unknown (RID: 0x{rid:X})";
        }

        static string GetSidType(string sid)
        {
            if (sid.StartsWith("S-1-16-"))
                return "Label";
            if (sid.StartsWith("S-1-5-32-"))
                return "Alias";
            if (sid.StartsWith("S-1-5-21-"))
                return "Group";
            if (sid.StartsWith("S-1-5-") || sid.StartsWith("S-1-1-") || sid.StartsWith("S-1-2-") || sid.StartsWith("S-1-18-"))
                return "Well-known group";
            if (sid.StartsWith("S-1-0-0"))
                return "Null SID";
            return "Unknown";
        }

        const int TOKEN_QUERY = 0x0008;
        const int TokenGroups = 2;
        const int TokenIntegrityLevel = 25;

        // Group Attributes
        // https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-token_groups_and_privileges
        const int SE_GROUP_MANDATORY = 0x00000001;
        const int SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        const int SE_GROUP_ENABLED = 0x00000004;
        const int SE_GROUP_OWNER = 0x00000008;
        const int SE_GROUP_USE_FOR_DENY_ONLY = 0x00000010;
        const int SE_GROUP_INTEGRITY = 0x00000020;
        const int SE_GROUP_INTEGRITY_ENABLED = 0x00000040;
        const int SE_GROUP_RESOURCE = 0x20000000;
        const int SE_GROUP_LOGON_ID = unchecked((int)0xC0000000);

        [StructLayout(LayoutKind.Sequential)]
        struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public int Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_GROUPS
        {
            public int GroupCount;
            public SID_AND_ATTRIBUTES Groups; // placeholder for first element
                                              // Followed by SID_AND_ATTRIBUTES[GroupCount]
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool ConvertSidToStringSid(IntPtr pSid, out IntPtr ptrSid);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(IntPtr hMem);

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Whoami", "whoami" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("Groups")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Show details about the current user."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Show the current user", "whoami"),
                    new ExampleEntry("List groups the current user is member of", "whoami -Groups")
                };
            }
        }
    }
}
