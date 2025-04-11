using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class GetWinStationCommand : PSCommand
    {
        public GetWinStationCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string serverName = _arguments.Get<StringArgument>("Server").Value;

            // Server handle
            IntPtr hServer = IntPtr.Zero;
            if (!string.IsNullOrEmpty(serverName))
                hServer = WinStationOpenServerW(serverName);

            // Call RPC function
            IntPtr sessionsPtr = IntPtr.Zero;
            int count = 0;
            uint ret = WinStationEnumerateW(hServer, ref sessionsPtr, ref count);
            int error = Marshal.GetLastWin32Error();
            if (ret != 1)
                throw new NoPowerShellException("WinStationEnumerateW failed: Error 0x{0:X8} connecting to server \"{1}\"", error, serverName);

            // Unmarshal result
            int structsize = Marshal.SizeOf(typeof(SESSIONIDW));
            SESSIONIDW[] sessions = new SESSIONIDW[count];

            //PrintLine("SESSIONNAME", null, "USERNAME", "ID", "STATE");
            for (int i = 0; i < count; i++)
            {
                IntPtr ins = new IntPtr(sessionsPtr.ToInt64() + (i * structsize));
                sessions[i] = (SESSIONIDW)Marshal.PtrToStructure(ins, typeof(SESSIONIDW));

                // Obtain more details on session
                int returnLength = 0;
                WINSTATIONINFORMATIONW wsInfo = new WINSTATIONINFORMATIONW();
                string domain = null;
                string username = null;
                DateTime connectTime = DateTime.MinValue;
                DateTime disconnectTime = DateTime.MinValue;
                DateTime logonTime = DateTime.MinValue;
                DateTime lastInputTime = DateTime.MinValue;
                DateTime currentTime = DateTime.MinValue;


                // Winstation information
                if (WinStationQueryInformationW(hServer, sessions[i].ID, WINSTATIONINFOCLASS.WinStationInformation, ref wsInfo, Marshal.SizeOf(typeof(WINSTATIONINFORMATIONW)), ref returnLength) != 0)
                {
                    domain = wsInfo.Domain;
                    username = wsInfo.UserName;

                    // ConnectTime
                    long connectTimeTicks = ((long)wsInfo.ConnectTime.dwHighDateTime << 32) + wsInfo.ConnectTime.dwLowDateTime;
                    connectTime = DateTime.FromFileTime(connectTimeTicks).ToUniversalTime();

                    // DisconnectTime
                    long disconnectTimeTicks = ((long)wsInfo.DisconnectTime.dwHighDateTime << 32) + wsInfo.DisconnectTime.dwLowDateTime;
                    disconnectTime = DateTime.FromFileTime(disconnectTimeTicks).ToUniversalTime();

                    // LogonTime
                    long logonTimeTicks = ((long)wsInfo.LoginTime.dwHighDateTime << 32) + wsInfo.LoginTime.dwLowDateTime;
                    logonTime = DateTime.FromFileTime(logonTimeTicks).ToUniversalTime();

                    // LastInputTime
                    long lastInputTimeTicks = ((long)wsInfo.LastInputTime.dwHighDateTime << 32) + wsInfo.LastInputTime.dwLowDateTime;
                    lastInputTime = DateTime.FromFileTime(lastInputTimeTicks).ToUniversalTime();

                    // CurrentTime
                    long currentTimeTicks = ((long)wsInfo.CurrentTime.dwHighDateTime << 32) + wsInfo.CurrentTime.dwLowDateTime;
                    currentTime = DateTime.FromFileTime(currentTimeTicks).ToUniversalTime();
                }
                else
                {
                    Program.WriteWarning("Error fetching winstation information for session {0}: 0x{1:X8}", sessions[i].ID, Marshal.GetLastWin32Error());
                }

                // Locked state
                bool locked = false;
                string locked_str = "Unknown";
                if (WinStationQueryInformationW(hServer, sessions[i].ID, WINSTATIONINFOCLASS.WinStationLockedState, ref locked, Marshal.SizeOf(typeof(bool)), ref returnLength) != 0)
                {
                    if (locked)
                        locked_str = "Yes";
                    else
                        locked_str = "No";
                }
                else
                {
                    Program.WriteWarning("Error fetching lockedstate information for session {0}: 0x{1:X8}", sessions[i].ID, Marshal.GetLastWin32Error());
                }

                // Remote address
                WINSTATIONREMOTEADDRESS address = new WINSTATIONREMOTEADDRESS();
                IPAddress remoteIP = null;
                if (WinStationQueryInformationW(hServer, sessions[i].ID, WINSTATIONINFOCLASS.WinStationRemoteAddress, ref address, Marshal.SizeOf(typeof(WINSTATIONREMOTEADDRESS)), ref returnLength) != 0)
                {
                    remoteIP = ExtractIPAddress(address.Family, address.Address);
                }
                else
                {
                    Program.WriteWarning("Error fetching remoteaddress information for session {0}: 0x{1:X8}", sessions[i].ID, Marshal.GetLastWin32Error());
                }

                // Store in results
                SESSIONIDW currentSession = sessions[i];
                _results.Add(
                    CompileResultRecord(
                        currentSession.WinStationName,
                        domain, username,
                        connectTime, disconnectTime, logonTime, lastInputTime, currentTime,
                        locked_str,
                        remoteIP,
                        currentSession.ID, currentSession.State
                    )
                );
            }

            // Always return the results so the output can be used by the next command in the pipeline
            return _results;
        }

        private static ResultRecord CompileResultRecord(
            string sessionname,
            string domain, string username,
            DateTime connectTime, DateTime disconnectTime, DateTime logonTime, DateTime lastInputTime, DateTime currentTime,
            string locked,
            IPAddress remoteIP,
            uint id, WINSTATIONSTATECLASS state
        )
        {
            string domainUser = string.Empty;
            if (!string.IsNullOrEmpty(domain))
            {
                if (!string.IsNullOrEmpty(username))
                    domainUser = string.Format("{0}\\{1}", domain, username);
            }
            else if (!string.IsNullOrEmpty(username))
            {
                domainUser = username;
            }

            DateTime minValue = new DateTime(1601, 1, 1);

            return new ResultRecord()
            {
                { "SessionName", sessionname },
                { "Username", domainUser },
                { "ID", id.ToString() },
                { "State", state.ToString() },
                { "Connected", connectTime != minValue ? connectTime.ToFormattedString() : string.Empty },
                { "Disconnected", disconnectTime != minValue ? disconnectTime.ToFormattedString() : string.Empty },
                { "LogonTime", logonTime != minValue ? logonTime.ToFormattedString() : string.Empty },
                { "LastInputTime", lastInputTime != minValue ? lastInputTime.ToFormattedString() : string.Empty },
                { "CurrentTime", currentTime != minValue ? currentTime.ToFormattedString() : string.Empty },
                { "Locked", locked },
                { "RemoteIP", remoteIP != null ? remoteIP.ToString() : string.Empty }
            };
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-WinStation", "qwinsta", "quser" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Display information about Remote Desktop Services sessions."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Query sessions on local machine", "Get-WinStation"),
                    new ExampleEntry
                    (
                        "Query sessions on a remote machine",
                        new List<string>()
                        {
                            "Get-WinStation -Server DC01.domain.local",
                            "qwinsta DC01.domain.local"
                        }
                    )
                };
            }
        }

        // Code inspired by https://github.com/gentilkiwi/mimikatz/ -> mimikatz\modules\kuhl_m_ts.{c,h}
        public enum WINSTATIONSTATECLASS
        {
            Active = 0,
            Connected = 1,
            ConnectQuery = 2,
            Shadow = 3,
            Disconnected = 4,
            Idle = 5,
            Listen = 6,
            Reset = 7,
            Down = 8,
            Init = 9
        }

        public enum WINSTATIONINFOCLASS
        {
            WinStationCreateData,
            WinStationConfiguration,
            WinStationPdParams,
            WinStationWd,
            WinStationPd,
            WinStationPrinter,
            WinStationClient,
            WinStationModules,
            WinStationInformation,
            WinStationTrace,
            WinStationBeep,
            WinStationEncryptionOff,
            WinStationEncryptionPerm,
            WinStationNtSecurity,
            WinStationUserToken,
            WinStationUnused1,
            WinStationVideoData,
            WinStationInitialProgram,
            WinStationCd,
            WinStationSystemTrace,
            WinStationVirtualData,
            WinStationClientData,
            WinStationSecureDesktopEnter,
            WinStationSecureDesktopExit,
            WinStationLoadBalanceSessionTarget,
            WinStationLoadIndicator,
            WinStationShadowInfo,
            WinStationDigProductId,
            WinStationLockedState,
            WinStationRemoteAddress,
            WinStationIdleTime,
            WinStationLastReconnectType,
            WinStationDisallowAutoReconnect,
            WinStationUnused2,
            WinStationUnused3,
            WinStationUnused4,
            WinStationUnused5,
            WinStationReconnectedFromId,
            WinStationEffectsPolicy,
            WinStationType,
            WinStationInformationEx
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SESSIONIDW
        {
            public UInt32 ID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
            public string WinStationName;
            public WINSTATIONSTATECLASS State;
        }

        // Structs from https://github.com/danports/cassia/ -> Source\Cassia\Impl\*.cs
        [StructLayout(LayoutKind.Sequential)]
        public struct PROTOCOLCOUNTERS
        {
            public int WdBytes;
            public int WdFrames;
            public int WaitForOutBuf;
            public int Frames;
            public int Bytes;
            public int CompressedBytes;
            public int CompressFlushes;
            public int Errors;
            public int Timeouts;
            public int AsyncFramingError;
            public int AsyncOverrunError;
            public int AsyncOverflowError;
            public int AsyncParityError;
            public int TdErrors;
            public short ProtocolType;
            public short Length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public int[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CACHE_STATISTICS
        {
            private readonly short ProtocolType;
            private readonly short Length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            private readonly int[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROTOCOLSTATUS
        {
            public PROTOCOLCOUNTERS Output;
            public PROTOCOLCOUNTERS Input;
            public CACHE_STATISTICS Statistics;
            public int AsyncSignal;
            public int AsyncSignalMask;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINSTATIONINFORMATIONW
        {
            public WINSTATIONSTATECLASS State;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
            public string WinStationName;

            public int SessionId;
            public int Unknown;
            public FILETIME ConnectTime;
            public FILETIME DisconnectTime;
            public FILETIME LastInputTime;
            public FILETIME LoginTime;
            public PROTOCOLSTATUS ProtocolStatus;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)]
            public string Domain;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string UserName;

            public FILETIME CurrentTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINSTATIONREMOTEADDRESS
        {
            public AddressFamily Family;
            public short Port;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] Reserved;
        }

        public static IPAddress ExtractIPAddress(AddressFamily family, byte[] rawAddress)
        {
            switch (family)
            {
                case AddressFamily.InterNetwork:
                    byte[] v4Address = new byte[4];
                    Array.Copy(rawAddress, 2, v4Address, 0, 4);
                    return new IPAddress(v4Address);
                case AddressFamily.InterNetworkV6:
                    byte[] v6Address = new byte[16];
                    Array.Copy(rawAddress, 2, v6Address, 0, 16);
                    return new IPAddress(v6Address);
            }

            return null;
        }

        // extern HANDLE WINAPI WinStationOpenServerW(IN PWSTR ServerName);
        [DllImport("winsta.dll", CharSet = CharSet.Auto)]
        static extern IntPtr WinStationOpenServerW(string ServerName);

        // extern BOOLEAN WINAPI WinStationEnumerateW(IN HANDLE hServer, OUT PSESSIONIDW *SessionIds, OUT PULONG Count);
        [DllImport("winsta.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint WinStationEnumerateW(IntPtr hServer, ref IntPtr sessions, ref int count);

        // extern BOOLEAN WINAPI WinStationQueryInformationW(IN HANDLE hServer, IN ULONG SessionId, IN WINSTATIONINFOCLASS WinStationInformationClass, OUT PVOID pWinStationInformation, IN ULONG WinStationInformationLength, OUT PULONG pReturnLength);
        [DllImport("winsta.dll", CharSet = CharSet.Auto)]
        static extern uint WinStationQueryInformationW(IntPtr hServer, uint SessionId, WINSTATIONINFOCLASS WinStationInformationClass, ref WINSTATIONINFORMATIONW pWinStationInformation, int WinStationInformationLength, ref int pReturnLength);

        [DllImport("winsta.dll", CharSet = CharSet.Auto)]
        static extern uint WinStationQueryInformationW(IntPtr hServer, uint SessionId, WINSTATIONINFOCLASS WinStationLockedState, ref bool locked, int WinStationLockedStateLength, ref int pReturnLength);

        [DllImport("winsta.dll", CharSet = CharSet.Auto)]
        static extern uint WinStationQueryInformationW(IntPtr hServer, uint SessionId, WINSTATIONINFOCLASS WinStationRemoteAddress, ref WINSTATIONREMOTEADDRESS address, int WinStationRemoteAddressLength, ref int pReturnLength);
    }
}
