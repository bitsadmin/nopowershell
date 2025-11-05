using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.LocalAccounts
{
    public class GetLocalGroupCommand : PSCommand
    {
        public GetLocalGroupCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string name = _arguments.Get<StringArgument>("Name").Value;
            string sid = _arguments.Get<StringArgument>("SID").Value;
            bool useWMI = _arguments.Get<BoolArgument>("UseWMI").Value;

            if (useWMI)
                return ExecuteWmi(name, sid);
            else
                return ExecutePInvoke(name, sid);
        }

        public CommandResult ExecuteWmi(string name, string sid)
        {
            string where = string.Empty;
            if (!string.IsNullOrEmpty(name))
                where = $" Where LocalAccount=True And Name='{name}'";
            else if (!string.IsNullOrEmpty(sid))
                where = $" Where LocalAccount=True And SID='{sid}'";
            else
                where = " Where LocalAccount=True";

            string query = $"Select Name, Description, SID From Win32_Group{where}";

            return WmiHelper.ExecuteWmiQuery(query, computername, username, password);
        }

        public CommandResult ExecutePInvoke(string name, string sid)
        {
            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                throw new NoPowerShellException("This implementation of retrieving groups via Netapi32!NetLocalGroupEnum does not support username and password. Use -UseWMI flag instead.");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(sid))
                throw new NoPowerShellException("The Name and SID parameters are mutually exclusive.");

            string serverForNet = string.IsNullOrEmpty(computername) ? null : @"\\" + computername;
            string serverForLookup = string.IsNullOrEmpty(computername) ? null : computername;

            string[] namePatterns = null;
            if (!string.IsNullOrEmpty(name))
            {
                namePatterns = name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            string[] sidList = null;
            if (!string.IsNullOrEmpty(sid))
            {
                sidList = sid.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }

            IntPtr bufPtr = IntPtr.Zero;
            uint entriesRead = 0;
            uint totalEntries = 0;
            uint resumeHandle = 0;
            uint ret = NetLocalGroupEnum(serverForNet, 1, out bufPtr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, ref resumeHandle);
            if (ret != NERR_Success)
            {
                throw new Win32Exception(unchecked((int)ret), $"NetLocalGroupEnum failed with error code: {ret}");
            }

            IntPtr currentPtr = bufPtr;
            for (uint i = 0; i < entriesRead; i++)
            {
                LOCALGROUP_INFO_1 lgi = Marshal.PtrToStructure<LOCALGROUP_INFO_1>(currentPtr);

                string gName = lgi.lpgri1_name;
                string description = lgi.lpgri1_comment ?? string.Empty;

                uint cbSid = 0;
                uint cchReferencedDomainName = 0;
                SID_NAME_USE peUse;
                bool success = LookupAccountName(serverForLookup, gName, null, ref cbSid, null, ref cchReferencedDomainName, out peUse);

                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_INSUFFICIENT_BUFFER)
                {
                    byte[] sidBytes = new byte[cbSid];
                    StringBuilder referencedDomainName = new StringBuilder((int)cchReferencedDomainName);

                    success = LookupAccountName(serverForLookup, gName, sidBytes, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out peUse);

                    if (success)
                    {
                        IntPtr sidPtr = Marshal.UnsafeAddrOfPinnedArrayElement(sidBytes, 0);
                        string gSid;
                        if (ConvertSidToStringSid(sidPtr, out gSid))
                        {
                            bool nameMatch = (namePatterns == null);
                            if (namePatterns != null)
                            {
                                foreach (string pat in namePatterns)
                                {
                                    string trimmedPat = pat.Trim();
                                    if (WildcardMatch(gName, trimmedPat))
                                    {
                                        nameMatch = true;
                                        break;
                                    }
                                }
                            }

                            bool sidMatch = (sidList == null);
                            if (sidList != null)
                            {
                                foreach (string psid in sidList)
                                {
                                    string trimmedSid = psid.Trim();
                                    if (string.Equals(gSid, trimmedSid, StringComparison.OrdinalIgnoreCase))
                                    {
                                        sidMatch = true;
                                        break;
                                    }
                                }
                            }

                            if (nameMatch && sidMatch)
                            {
                                _results.Add(
                                    new ResultRecord()
                                    {
                                        { "Name", gName },
                                        { "Description", description },
                                        { "SID", gSid },
                                        //{ "PrincipalSource", "Local" },
                                        //{ "ObjectClass", "Group" }
                                    }
                                );
                            }
                        }
                        else
                        {
                            err = Marshal.GetLastWin32Error();
                            throw new Win32Exception(err, $"ConvertSidToStringSid failed for group {gName}");
                        }
                    }
                    else
                    {
                        err = Marshal.GetLastWin32Error();
                        throw new Win32Exception(err, $"LookupAccountName failed for group {gName}");
                    }
                }
                else
                {
                    throw new Win32Exception(err, $"Initial LookupAccountName failed for group {gName}");
                }

                currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<LOCALGROUP_INFO_1>());
            }

            NetApiBufferFree(bufPtr);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-LocalGroup" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name", true),
                    new StringArgument("SID", true),
                    new BoolArgument("UseWMI")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets local security groups."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Gets all the local groups on the computer", "Get-LocalGroup"),
                    new ExampleEntry("Gets the local group with the name Administrators", "Get-LocalGroup -Name Administrators"),
                    new ExampleEntry("Gets all the local groups that match the name pattern", "Get-LocalGroup -Name Admin* | fl"),
                    new ExampleEntry("Gets the local group that has the specified SID", "Get-LocalGroup -SID S-1-5-32-544"),
                    new ExampleEntry("Gets all the local groups on a remote computer", "Get-LocalGroup -ComputerName MyServer"),
                    new ExampleEntry("Gets all the local groups on a remote computer using WMI instead of Netapi32!NetLocalGroupEnum", "Get-LocalGroup -UseWMI -ComputerName MyServer -Username LabAdmin -Password Password1!"),
                };
            }
        }

        private static bool WildcardMatch(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return false;

            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }

        private const uint NERR_Success = 0;
        private const uint MAX_PREFERRED_LENGTH = uint.MaxValue;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint NetLocalGroupEnum(
            string servername,
            uint level,
            out IntPtr bufptr,
            uint prefmaxlen,
            out uint entriesread,
            out uint totalentries,
            ref uint resume_handle);

        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern uint NetApiBufferFree(IntPtr buffer);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupAccountName(
            string lpSystemName,
            string lpAccountName,
            byte[] lpSid,
            ref uint cbSid,
            StringBuilder lpReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LOCALGROUP_INFO_1
        {
            public string lpgri1_name;
            public string lpgri1_comment;
        }

        private enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer,
            SidTypeLabel
        }
    }
}