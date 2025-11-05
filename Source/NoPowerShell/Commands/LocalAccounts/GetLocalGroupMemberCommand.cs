using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.LocalAccounts
{
    public class GetLocalGroupMemberCommand : PSCommand
    {
        public GetLocalGroupMemberCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string name = _arguments.Get<StringArgument>("Name").Value;
            string sid = _arguments.Get<StringArgument>("SID").Value;
            bool useWMI = _arguments.Get<BoolArgument>("UseWMI").Value;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(sid))
                throw new NoPowerShellException("Specify either the Name or SID parameter.");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(sid))
                throw new NoPowerShellException("The Name and SID parameters are mutually exclusive.");

            if (useWMI)
                return ExecuteWmi(name, sid);
            else
                return ExecutePInvoke(name, sid);
        }

        public CommandResult ExecuteWmi(string name, string sid)
        {
            string groupName = null;
            string groupDomain = null;

            string compName = computername == "." ? Environment.MachineName : computername;

            if (!string.IsNullOrEmpty(sid))
            {
                string query = $"Select Name, Domain From Win32_Group Where SID='{sid}'";
                CommandResult groupRes = WmiHelper.ExecuteWmiQuery(query, computername, username, password);
                if (groupRes.Count == 0)
                    throw new NoPowerShellException($"No group found with SID '{sid}'.");
                groupName = groupRes[0]["Name"];
                groupDomain = groupRes[0]["Domain"];
            }
            else if (!string.IsNullOrEmpty(name))
            {
                groupName = name;
                groupDomain = compName;
            }

            string membersQuery = $"ASSOCIATORS OF {{Win32_Group.Domain='{groupDomain}',Name='{groupName}'}} WHERE ResultClass = Win32_Account";
            CommandResult members = WmiHelper.ExecuteWmiQuery(membersQuery, computername, username, password);

            foreach (ResultRecord member in members)
            {
                string fullName = member["Caption"];
                string memberSid = member["SID"];
                string objectClass = ((SID_NAME_USE)Convert.ToInt32(member["SIDType"])).ToString();
                string partDomain = member["Domain"];
                string principalSource;
                if (string.Equals(partDomain, "BUILTIN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(partDomain, "NT AUTHORITY", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(partDomain, compName, StringComparison.OrdinalIgnoreCase))
                {
                    principalSource = "Local";
                }
                else
                {
                    principalSource = "ActiveDirectory";
                }

                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", fullName },
                        { "SID", memberSid },
                        { "ObjectClass", objectClass },
                        { "PrincipalSource", principalSource }
                    }
                );
            }
            return _results;
        }

        public CommandResult ExecutePInvoke(string name, string sid)
        {
            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                throw new NoPowerShellException("This implementation of retrieving group members via Netapi32!NetLocalGroupGetMembers does not support username and password. Use -UseWMI flag instead.");

            string server = string.IsNullOrEmpty(computername) ? null : @"\\" + computername;
            string compName = string.IsNullOrEmpty(computername) ? Environment.MachineName : computername;

            string groupName = null;

            if (!string.IsNullOrEmpty(sid))
            {
                IntPtr pSid = IntPtr.Zero;
                if (!ConvertStringSidToSid(sid, out pSid))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                try
                {
                    StringBuilder nameBuilder = new StringBuilder(256);
                    uint nameSize = (uint)nameBuilder.Capacity;
                    StringBuilder domainBuilder = new StringBuilder(256);
                    uint domainSize = (uint)domainBuilder.Capacity;
                    SID_NAME_USE sidUse;

                    bool success = LookupAccountSid(server, pSid, nameBuilder, ref nameSize, domainBuilder, ref domainSize, out sidUse);
                    if (!success)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error == ERROR_INSUFFICIENT_BUFFER)
                        {
                            nameBuilder.Capacity = (int)nameSize;
                            domainBuilder.Capacity = (int)domainSize;
                            success = LookupAccountSid(server, pSid, nameBuilder, ref nameSize, domainBuilder, ref domainSize, out sidUse);
                            if (!success)
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                        else
                        {
                            throw new Win32Exception(error);
                        }
                    }

                    if (sidUse != SID_NAME_USE.Group && sidUse != SID_NAME_USE.Alias && sidUse != SID_NAME_USE.WellKnownGroup)
                        throw new NoPowerShellException($"SID '{sid}' does not belong to a group.");

                    groupName = nameBuilder.ToString();
                }
                finally
                {
                    if (pSid != IntPtr.Zero)
                        LocalFree(pSid);
                }
            }
            else if (!string.IsNullOrEmpty(name))
            {
                groupName = name;
            }

            IntPtr bufPtr = IntPtr.Zero;
            uint entriesRead = 0;
            uint totalEntries = 0;
            uint resumeHandle = 0;

            uint ret = NetLocalGroupGetMembers(server, groupName, 2, out bufPtr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, ref resumeHandle);
            if (ret != NERR_Success)
            {
                if (bufPtr != IntPtr.Zero)
                    NetApiBufferFree(bufPtr);
                throw new Win32Exception(unchecked((int)ret), $"NetLocalGroupGetMembers failed with error code: {ret}");
            }

            try
            {
                IntPtr currentPtr = bufPtr;
                for (uint i = 0; i < entriesRead; i++)
                {
                    LOCALGROUP_MEMBERS_INFO_2 mi = Marshal.PtrToStructure<LOCALGROUP_MEMBERS_INFO_2>(currentPtr);

                    string memberSid = null;
                    if (!ConvertSidToStringSid(mi.lgrmi2_sid, out memberSid))
                        memberSid = string.Empty;

                    string fullName = mi.lgrmi2_domainandname;

                    string objectClass = (mi.lgrmi2_sidusage == (uint)SID_NAME_USE.User) ? "User" : "Group";

                    string principalSource;
                    int slashIndex = fullName.IndexOf('\\');
                    string partDomain = (slashIndex != -1) ? fullName.Substring(0, slashIndex) : string.Empty;

                    if (string.Equals(partDomain, "BUILTIN", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(partDomain, "NT AUTHORITY", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(partDomain, compName, StringComparison.OrdinalIgnoreCase))
                    {
                        principalSource = "Local";
                    }
                    else
                    {
                        principalSource = "ActiveDirectory";
                    }

                    _results.Add(
                        new ResultRecord()
                        {
                            { "Name", fullName },
                            { "SID", memberSid },
                            { "ObjectClass", objectClass },
                            { "PrincipalSource", principalSource }
                        }
                    );

                    currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<LOCALGROUP_MEMBERS_INFO_2>());
                }
            }
            finally
            {
                if (bufPtr != IntPtr.Zero)
                    NetApiBufferFree(bufPtr);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-LocalGroupMember" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name", true),
                    new StringArgument("SID", true),
                    new BoolArgument("UseWMI") // Unofficial
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the members of local groups."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Gets the members of the local group Administrators", "Get-LocalGroupMember -Name Administrators"),
                    new ExampleEntry("Gets the members of the local group with the specified SID", "Get-LocalGroupMember -SID S-1-5-32-544"),
                    new ExampleEntry("Gets the members of a local group on a remote computer", "Get-LocalGroupMember -Name Administrators -ComputerName MyServer"),
                    new ExampleEntry("Gets the members of a local group on a remote computer using WMI", "Get-LocalGroupMember -UseWMI -Name Administrators -ComputerName MyServer -Username LabAdmin -Password Password1!")
                };
            }
        }

        private const uint NERR_Success = 0;
        private const uint MAX_PREFERRED_LENGTH = uint.MaxValue;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        private enum SID_NAME_USE : uint
        {
            User = 1,
            Group = 2,
            Domain = 3,
            Alias = 4,
            WellKnownGroup = 5,
            DeletedAccount = 6,
            Invalid = 7,
            Unknown = 8,
            Computer = 9,
            Label = 10
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LOCALGROUP_MEMBERS_INFO_2
        {
            public IntPtr lgrmi2_sid;
            public uint lgrmi2_sidusage;
            public string lgrmi2_domainandname;
        }

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint NetLocalGroupGetMembers(
            string servername,
            string localgroupname,
            uint level,
            out IntPtr bufptr,
            uint prefmaxlen,
            out uint entriesread,
            out uint totalentries,
            ref uint resume_handle);

        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern uint NetApiBufferFree(IntPtr buffer);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupAccountSid(
            string lpSystemName,
            IntPtr Sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);
    }
}