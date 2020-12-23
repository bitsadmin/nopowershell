using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class QueryWinStationCommand : PSCommand
    {
        public QueryWinStationCommand(string[] userArguments) : base(userArguments, SupportedArguments)
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
                throw new NoPowerShellException("WinStationEnumerateW failed, error 0x{0:X8}", error);

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
                if (WinStationQueryInformationW(hServer, sessions[i].ID, WINSTATIONINFOCLASS.WinStationInformation, ref wsInfo, Marshal.SizeOf(typeof(WINSTATIONINFORMATIONW)), ref returnLength) != 0)
                {
                    domain = wsInfo.Domain;
                    username = wsInfo.UserName;
                }

                // Store in results
                SESSIONIDW currentSession = sessions[i];
                _results.Add(CompileResultRecord(currentSession.WinStationName, domain, username, currentSession.ID, currentSession.State));
            }

            // Always return the results so the output can be used by the next command in the pipeline
            return _results;
        }

        private static ResultRecord CompileResultRecord(string sessionname, string domain, string username, uint id, WINSTATIONSTATECLASS state)
        {
            string domainUser = string.Empty;
            if(!string.IsNullOrEmpty(domain))
            {
                if (!string.IsNullOrEmpty(username))
                    domainUser = string.Format("{0}\\{1}", domain, username);
            }
            else if(!string.IsNullOrEmpty(username))
            {
                domainUser = username;
            }

            return new ResultRecord()
            {
                { "SessionName", sessionname },
                { "Username", domainUser },
                { "ID", id.ToString() },
                { "State", state.ToString() }
            };
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Query-WinStation", "qwinsta", "quser" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server")
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
                    new ExampleEntry("Query sessions on local machine", "Query-WinStation"),
                    new ExampleEntry
                    (
                        "Query sessions on a remote machine",
                        new List<string>()
                        {
                            "Query-WinStation -Server DC01.domain.local",
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

        // extern HANDLE WINAPI WinStationOpenServerW(IN PWSTR ServerName);
        [DllImport("winsta.dll", CharSet = CharSet.Auto)]
        static extern IntPtr WinStationOpenServerW(string ServerName);

        // extern BOOLEAN WINAPI WinStationEnumerateW(IN HANDLE hServer, OUT PSESSIONIDW *SessionIds, OUT PULONG Count);
        [DllImport("winsta.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern uint WinStationEnumerateW(IntPtr hServer, ref IntPtr sessions, ref int count);

        // extern BOOLEAN WINAPI WinStationQueryInformationW(IN HANDLE hServer, IN ULONG SessionId, IN WINSTATIONINFOCLASS WinStationInformationClass, OUT PVOID pWinStationInformation, IN ULONG WinStationInformationLength, OUT PULONG pReturnLength);
        [DllImport("winsta.dll", CharSet = CharSet.Auto)]
        static extern uint WinStationQueryInformationW(IntPtr hServer, uint SessionId, WINSTATIONINFOCLASS WinStationInformationClass, ref WINSTATIONINFORMATIONW pWinStationInformation, int WinStationInformationLength, ref int pReturnLength);
    }
}
