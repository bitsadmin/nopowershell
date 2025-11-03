using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Security.Principal;

namespace NoPowerShell.Commands
{
    public class GetLocalUserCommand : PSCommand
    {
        public GetLocalUserCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Fetch computername, username, password parameters
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
            string query = "Select Name, Disabled, Description{0} From Win32_UserAccount{1}";

            if (!string.IsNullOrEmpty(name))
                query = string.Format(query, ", SID", string.Format(" Where Name='{0}'", name));
            else if (!string.IsNullOrEmpty(sid))
                query = string.Format(query, ", SID", string.Format(" Where SID='{0}'", sid));
            else
                query = string.Format(query, string.Empty, string.Empty);

            return WmiHelper.ExecuteWmiQuery(query, computername, username, password);
        }

        public CommandResult ExecutePInvoke(string name, string sid)
        {
            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                throw new NoPowerShellException("This implementation of retrieving users via Netapi32!NetUserEnum does not support username and password. Use -UseWMI flag instead.");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(sid))
                throw new NoPowerShellException("The Name and SID parameters are mutually exclusive.");

            string server = string.IsNullOrEmpty(computername) ? null : @"\\" + computername; // Unofficial

            bool impersonating = false;
            IntPtr userToken = IntPtr.Zero;
            WindowsImpersonationContext impersonationContext = null;

            try
            {
                string domainSid = GetAccountDomainSid(computername);

                IntPtr modalsPtr = IntPtr.Zero;
                uint ret = NetUserModalsGet(server, 0, out modalsPtr);
                if (ret != NERR_Success)
                {
                    throw new Win32Exception(unchecked((int)ret), $"NetUserModalsGet failed with error code: {ret}");
                }
                USER_MODALS_INFO_0 modals = Marshal.PtrToStructure<USER_MODALS_INFO_0>(modalsPtr);
                uint maxPasswdAge = modals.usrmod0_max_passwd_age;
                uint minPasswdAge = modals.usrmod0_min_passwd_age;
                NetApiBufferFree(modalsPtr);

                IntPtr bufPtr = IntPtr.Zero;
                uint entriesRead = 0;
                uint totalEntries = 0;
                uint resumeHandle = 0;
                ret = NetUserEnum(server, 3, FILTER_NORMAL_ACCOUNT, out bufPtr, uint.MaxValue, out entriesRead, out totalEntries, ref resumeHandle);
                if (ret != NERR_Success)
                {
                    throw new Win32Exception(unchecked((int)ret), $"NetUserEnum failed with error code: {ret}");
                }

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

                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                double nowSeconds = (DateTime.UtcNow - unixEpoch).TotalSeconds;

                IntPtr currentPtr = bufPtr;
                for (uint i = 0; i < entriesRead; i++)
                {
                    USER_INFO_3 ui = Marshal.PtrToStructure<USER_INFO_3>(currentPtr);

                    string uName = ui.name;
                    string uSid = domainSid + "-" + ui.user_id;

                    bool nameMatch = (namePatterns == null);
                    if (namePatterns != null)
                    {
                        foreach (string pat in namePatterns)
                        {
                            string trimmedPat = pat.Trim();
                            if (WildcardMatch(uName, trimmedPat))
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
                            if (string.Equals(uSid, trimmedSid, StringComparison.OrdinalIgnoreCase))
                            {
                                sidMatch = true;
                                break;
                            }
                        }
                    }

                    if (!nameMatch || !sidMatch)
                    {
                        currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<USER_INFO_3>());
                        continue;
                    }

                    DateTime passwordLastSet = unixEpoch.AddSeconds(nowSeconds - ui.password_age);

                    DateTime? passwordChangeableDate = passwordLastSet.AddSeconds(minPasswdAge);

                    DateTime? passwordExpires = null;
                    bool dontExpire = (ui.flags & UF_DONT_EXPIRE_PASSWD) != 0 || maxPasswdAge == TIMEQ_FOREVER;
                    if (!dontExpire)
                    {
                        passwordExpires = passwordLastSet.AddSeconds(maxPasswdAge);
                    }

                    DateTime? accountExpires = (ui.acct_expires == TIMEQ_FOREVER) ? (DateTime?)null : unixEpoch.AddSeconds(ui.acct_expires);

                    DateTime? lastLogon = (ui.last_logon == 0) ? (DateTime?)null : unixEpoch.AddSeconds(ui.last_logon);

                    bool enabled = (ui.flags & UF_ACCOUNTDISABLE) == 0;
                    bool userMayChangePassword = (ui.flags & UF_PASSWD_CANT_CHANGE) == 0;
                    bool passwordRequired = (ui.flags & UF_PASSWD_NOTREQD) == 0;

                    string description = ui.comment ?? string.Empty;
                    string fullName = ui.full_name ?? string.Empty;

                    _results.Add(
                        new ResultRecord()
                        {
                            { "Name", uName },
                            { "Enabled", enabled.ToString() },
                            { "Description", description },
                            { "FullName", fullName },
                            { "LastLogon", lastLogon?.ToFormattedString() },
                            { "PasswordRequired", passwordRequired.ToString() },
                            { "PasswordLastSet", passwordLastSet.ToFormattedString() },
                            { "AccountExpires", accountExpires?.ToFormattedString() },
                            { "PasswordChangeableDate", passwordChangeableDate?.ToFormattedString() },
                            { "PasswordExpires", passwordExpires?.ToFormattedString() },
                            { "UserMayChangePassword", userMayChangePassword.ToString() },
                            { "SID", uSid }
                            //{ "PrincipalSource", "Local" },
                            //{ "ObjectClass", "User" }
                        }
                    );

                    currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<USER_INFO_3>());
                }

                NetApiBufferFree(bufPtr);
            }
            catch (Exception e)
            {
                throw new NoPowerShellException(e.Message);
            }
            finally
            {
                if (impersonating)
                    impersonationContext.Undo();

                if (userToken != IntPtr.Zero)
                    CloseHandle(userToken);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-LocalUser", "glu" }; }
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
            get { return "Gets local user accounts."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Gets all the local user accounts on the computer", "Get-LocalUser | select Name,Enabled,Description"),
                    new ExampleEntry("Gets the local user account with the name Administrator", "Get-LocalUser -Name Administrator"),
                    new ExampleEntry("Gets all the local user accounts that match the name pattern", "Get-LocalUser -Name Admin* | fl"),
                    new ExampleEntry("Gets the local user account that has the specified SID", "Get-LocalUser -SID S-1-5-21-222222222-3333333333-4444444444-5555"),
                    new ExampleEntry("Gets all the local user accounts on a remote computer", "Get-LocalUser -ComputerName MyServer | select Name,Enabled,Description"),
                    new ExampleEntry("Gets all the local user accounts on a remote computer using WMI instead of Netapi32!NetUserEnum", "Get-LocalUser -UseWMI -ComputerName MyServer -Username LabAdmin -Password Password1!"),
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

        private static string GetAccountDomainSid(string computerName)
        {
            LSA_UNICODE_STRING systemName = default;
            IntPtr namePtr = IntPtr.Zero;
            if (!string.IsNullOrEmpty(computerName))
            {
                namePtr = Marshal.StringToHGlobalUni(computerName);
                systemName.buffer = namePtr;
                systemName.Length = (ushort)(computerName.Length * 2);
                systemName.MaximumLength = systemName.Length;
            }

            LSA_OBJECT_ATTRIBUTES objectAttributes = new LSA_OBJECT_ATTRIBUTES();
            objectAttributes.Length = Marshal.SizeOf(typeof(LSA_OBJECT_ATTRIBUTES));

            IntPtr policyHandle = IntPtr.Zero;
            uint status = LsaOpenPolicy(ref systemName, ref objectAttributes, POLICY_VIEW_LOCAL_INFORMATION, out policyHandle);
            if (status != 0)
                throw new Win32Exception(unchecked((int)status));

            IntPtr infoPtr = IntPtr.Zero;
            status = LsaQueryInformationPolicy(policyHandle, POLICY_ACCOUNT_DOMAIN_INFORMATION, out infoPtr);
            if (status != 0)
            {
                LsaClose(policyHandle);
                throw new Win32Exception(unchecked((int)status));
            }

            POLICY_ACCOUNT_DOMAIN_INFO info = Marshal.PtrToStructure<POLICY_ACCOUNT_DOMAIN_INFO>(infoPtr);

            string sidString = null;
            if (!ConvertSidToStringSid(info.DomainSid, out sidString))
            {
                int err = Marshal.GetLastWin32Error();
                LsaFreeMemory(infoPtr);
                LsaClose(policyHandle);
                throw new Win32Exception(err);
            }

            LsaFreeMemory(infoPtr);
            LsaClose(policyHandle);

            if (namePtr != IntPtr.Zero)
                Marshal.FreeHGlobal(namePtr);

            return sidString;
        }

        private const uint NERR_Success = 0;
        private const uint FILTER_NORMAL_ACCOUNT = 0x0200;
        private const uint TIMEQ_FOREVER = uint.MaxValue;
        private const uint UF_ACCOUNTDISABLE = 0x00000002;
        private const uint UF_PASSWD_NOTREQD = 0x00000020;
        private const uint UF_PASSWD_CANT_CHANGE = 0x00000040;
        private const uint UF_DONT_EXPIRE_PASSWD = 0x00010000;
        private const uint POLICY_VIEW_LOCAL_INFORMATION = 0x00000001;
        private const uint POLICY_ACCOUNT_DOMAIN_INFORMATION = 5;

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint NetUserEnum(
            string servername,
            uint level,
            uint filter,
            out IntPtr bufptr,
            uint prefmaxlen,
            out uint entriesread,
            out uint totalentries,
            ref uint resume_handle);

        [DllImport("netapi32.dll", SetLastError = true)]
        private static extern uint NetApiBufferFree(IntPtr buffer);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint NetUserModalsGet(
            string servername,
            uint level,
            out IntPtr bufptr);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertSidToStringSid(IntPtr pSid, out string strSid);

        [DllImport("advapi32.dll", PreserveSig = true)]
        private static extern uint LsaOpenPolicy(
            ref LSA_UNICODE_STRING SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            uint DesiredAccess,
            out IntPtr PolicyHandle);

        [DllImport("advapi32.dll", PreserveSig = true)]
        private static extern uint LsaQueryInformationPolicy(
            IntPtr PolicyHandle,
            uint PolicyInformationClass,
            out IntPtr Buffer);

        [DllImport("advapi32.dll", PreserveSig = true)]
        private static extern uint LsaFreeMemory(IntPtr Buffer);

        [DllImport("advapi32.dll", PreserveSig = true)]
        private static extern uint LsaClose(IntPtr PolicyHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct USER_INFO_3
        {
            public string name;
            public string password;
            public uint password_age;
            public uint priv;
            public string home_dir;
            public string comment;
            public uint flags;
            public string script_path;
            public uint auth_flags;
            public string full_name;
            public string usr_comment;
            public string parms;
            public string workstations;
            public uint last_logon;
            public uint last_logoff;
            public uint acct_expires;
            public uint max_storage;
            public uint units_per_week;
            public IntPtr logon_hours;
            public uint bad_pw_count;
            public uint num_logons;
            public string logon_server;
            public uint country_code;
            public uint code_page;
            public uint user_id;
            public uint primary_group_id;
            public string profile;
            public string home_dir_drive;
            public uint password_expired;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct USER_MODALS_INFO_0
        {
            public uint usrmod0_min_passwd_len;
            public uint usrmod0_max_passwd_age;
            public uint usrmod0_min_passwd_age;
            public uint usrmod0_force_logoff;
            public uint usrmod0_password_hist_len;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LSA_UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LSA_OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POLICY_ACCOUNT_DOMAIN_INFO
        {
            public LSA_UNICODE_STRING DomainName;
            public IntPtr DomainSid;
        }
    }
}