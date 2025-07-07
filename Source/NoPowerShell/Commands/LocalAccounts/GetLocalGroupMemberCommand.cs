using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Runtime.InteropServices;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.LocalAccounts
{
    // Most code in this class is from https://www.codeproject.com/Articles/2937/Getting-local-groups-and-member-names-in-C
    // Code from obtaining the SID string by Igor Korkhov from https://stackoverflow.com/a/2146418

    internal class Win32API
    {
        #region Win32 API Interfaces
        [DllImport("netapi32.dll", EntryPoint = "NetApiBufferFree")]
        internal static extern void NetApiBufferFree(IntPtr bufptr);

        [DllImport("netapi32.dll", EntryPoint = "NetLocalGroupGetMembers")]
        internal static extern uint NetLocalGroupGetMembers(
            IntPtr ServerName,
            IntPtr GrouprName,
            uint level,
            ref IntPtr siPtr,
            uint prefmaxlen,
            ref uint entriesread,
            ref uint totalentries,
            IntPtr resumeHandle);

        [DllImport("netapi32.dll", EntryPoint = "NetLocalGroupEnum")]
        internal static extern uint NetLocalGroupEnum(
            IntPtr ServerName,
            uint level,
            ref IntPtr siPtr,
            uint prefmaxlen,
            ref uint entriesread,
            ref uint totalentries,
            IntPtr resumeHandle);

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct LOCALGROUP_MEMBERS_INFO_2
        {
            public IntPtr lgrmi2_sid;
            public IntPtr lgrmi2_sidusage;
            public IntPtr lgrmi2_name;
        }

        [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct LOCALGROUP_INFO_1
        {
            public IntPtr lpszGroupName;
            public IntPtr lpszComment;
        }

        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr LocalFree(IntPtr hMem);
        #endregion
    }

    public class GetLocalGroupMemberCommand : PSCommand
    {
        public GetLocalGroupMemberCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Parameters
            string group = _arguments.Get<StringArgument>("Group").Value;
            string name = _arguments.Get<StringArgument>("Name").Value;

            // Validate parameters
            string groupname = group ?? name;
            if (string.IsNullOrEmpty(groupname))
                throw new NoPowerShellException("-Group or -Name parameter needs to be provided");

            // Defining values for getting group names
            uint level = 1, prefmaxlen = 0xFFFFFFFF, entriesread = 0, totalentries = 0;
            int LOCALGROUP_INFO_1_SIZE, LOCALGROUP_MEMBERS_INFO_2_SIZE;
            unsafe
            {
                LOCALGROUP_INFO_1_SIZE = sizeof(Win32API.LOCALGROUP_INFO_1);
                LOCALGROUP_MEMBERS_INFO_2_SIZE = sizeof(Win32API.LOCALGROUP_MEMBERS_INFO_2);
            }

            // Values that will receive information
            IntPtr GroupInfoPtr, UserInfoPtr;
            GroupInfoPtr = IntPtr.Zero;
            UserInfoPtr = IntPtr.Zero;

            Win32API.NetLocalGroupEnum(
                IntPtr.Zero,            // Server name.it must be null
                level,                  // Level can be 0 or 1 for groups. For more information see LOCALGROUP_INFO_0 and LOCALGROUP_INFO_1
                ref GroupInfoPtr,       // Value that will be receive information
                prefmaxlen,             // Maximum length
                ref entriesread,        // Value that receives the count of elements actually enumerated
                ref totalentries,       // Value that receives the approximate total number of entries that could have been enumerated from the current resume position
                IntPtr.Zero
            );

            // This string array will hold comments of each group
            string[] commentArray = new string[totalentries];

            // Getting group names
            bool found = false;
            for (int i = 0; i < totalentries; i++)
            {
                // Converting unmanaged code to managed codes with using Marshal class 
                int newOffset = GroupInfoPtr.ToInt32() + LOCALGROUP_INFO_1_SIZE * i;
                Win32API.LOCALGROUP_INFO_1 groupInfo = (Win32API.LOCALGROUP_INFO_1)Marshal.PtrToStructure(new IntPtr(newOffset), typeof(Win32API.LOCALGROUP_INFO_1));
                string currentGroupName = Marshal.PtrToStringAuto(groupInfo.lpszGroupName);

                if (groupname.ToLowerInvariant() != currentGroupName.ToLowerInvariant())
                    continue;

                found = true;

                // Defining value for getting name of members in each group
                uint prefmaxlen1 = 0xFFFFFFFF, entriesread1 = 0, totalentries1 = 0;

                // Parameters for NetLocalGroupGetMembers is like NetLocalGroupEnum
                Win32API.NetLocalGroupGetMembers(
                    IntPtr.Zero,
                    groupInfo.lpszGroupName, 2,
                    ref UserInfoPtr, prefmaxlen1,
                    ref entriesread1, ref totalentries1,
                    IntPtr.Zero
                );

                // Getting member's name
                for (int j = 0; j < totalentries1; j++)
                {
                    // Converting unmanaged code to managed codes using Marshal class 
                    int newOffset1 = UserInfoPtr.ToInt32() + LOCALGROUP_MEMBERS_INFO_2_SIZE * j;
                    Win32API.LOCALGROUP_MEMBERS_INFO_2 memberInfo = (Win32API.LOCALGROUP_MEMBERS_INFO_2)Marshal.PtrToStructure(new IntPtr(newOffset1), typeof(Win32API.LOCALGROUP_MEMBERS_INFO_2));
                    string currentUserName = Marshal.PtrToStringAuto(memberInfo.lgrmi2_name);
                    IntPtr pstr = IntPtr.Zero;
                    Win32API.ConvertSidToStringSid(memberInfo.lgrmi2_sid, out pstr);
                    string sid = Marshal.PtrToStringAuto(pstr);
                    Win32API.LocalFree(pstr);

                    _results.Add(
                        new ResultRecord()
                        {
                            { "Name", currentUserName },
                            { "SID", sid }
                        }
                    );
                }
                // Free memory
                Win32API.NetApiBufferFree(UserInfoPtr);
            }
            // Free memory
            Win32API.NetApiBufferFree(GroupInfoPtr);

            if (!found)
                throw new NoPowerShellException("Group {0} was not found.", groupname);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-LocalGroupMember", "glgm" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Group"),
                    new StringArgument("Name", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets members from a local group."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List members of the Administrators group", "Get-LocalGroupMember -Group Administrators")
                };
            }
        }
    }
}
