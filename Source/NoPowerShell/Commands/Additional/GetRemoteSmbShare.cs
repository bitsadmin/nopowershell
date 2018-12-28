using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetRemoteSmbShare : PSCommand
    {
        public GetRemoteSmbShare(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string server = _arguments.Get<StringArgument>("Server").Value;
            server = server.Replace(@"\\", "");

            GetNetShares gns = new GetNetShares();
            GetNetShares.SHARE_INFO_1[] shares = gns.EnumNetShares(server);

            foreach(GetNetShares.SHARE_INFO_1 share in shares)
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", share.shi1_netname },
                        { "Description", share.shi1_remark }
                        //{ "Type", share.shi1_type.ToString() }
                    }
                );
            }

            return _results;
        }

        // Source: https://www.pinvoke.net/default.aspx/netapi32/netshareenum.html
        public class GetNetShares
        {
            #region External Calls
            [DllImport("Netapi32.dll", SetLastError = true)]
            static extern int NetApiBufferFree(IntPtr Buffer);
            [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
            private static extern int NetShareEnum(
                 StringBuilder ServerName,
                 int level,
                 ref IntPtr bufPtr,
                 uint prefmaxlen,
                 ref int entriesread,
                 ref int totalentries,
                 ref int resume_handle
                 );
            #endregion
            #region External Structures
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SHARE_INFO_1
            {
                public string shi1_netname;
                public uint shi1_type;
                public string shi1_remark;
                public SHARE_INFO_1(string sharename, uint sharetype, string remark)
                {
                    this.shi1_netname = sharename;
                    this.shi1_type = sharetype;
                    this.shi1_remark = remark;
                }
                public override string ToString()
                {
                    return shi1_netname;
                }
            }
            #endregion
            const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;
            const int NERR_Success = 0;
            private enum NetError : uint
            {
                NERR_Success = 0,
                NERR_BASE = 2100,
                NERR_UnknownDevDir = (NERR_BASE + 16),
                NERR_DuplicateShare = (NERR_BASE + 18),
                NERR_BufTooSmall = (NERR_BASE + 23),
            }
            private enum SHARE_TYPE : uint
            {
                STYPE_DISKTREE = 0,
                STYPE_PRINTQ = 1,
                STYPE_DEVICE = 2,
                STYPE_IPC = 3,
                STYPE_SPECIAL = 0x80000000,
            }
            public SHARE_INFO_1[] EnumNetShares(string Server)
            {
                List<SHARE_INFO_1> ShareInfos = new List<SHARE_INFO_1>();
                int entriesread = 0;
                int totalentries = 0;
                int resume_handle = 0;
                int nStructSize = Marshal.SizeOf(typeof(SHARE_INFO_1));
                IntPtr bufPtr = IntPtr.Zero;
                StringBuilder server = new StringBuilder(Server);
                int ret = NetShareEnum(server, 1, ref bufPtr, MAX_PREFERRED_LENGTH, ref entriesread, ref totalentries, ref resume_handle);
                if (ret == NERR_Success)
                {
                    IntPtr currentPtr = bufPtr;
                    for (int i = 0; i < entriesread; i++)
                    {
                        SHARE_INFO_1 shi1 = (SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(SHARE_INFO_1));
                        ShareInfos.Add(shi1);
                        currentPtr = new IntPtr(currentPtr.ToInt64() + nStructSize); // Updated to support .NET 2.0
                    }
                    NetApiBufferFree(bufPtr);
                    return ShareInfos.ToArray();
                }
                else
                {
                    ShareInfos.Add(new SHARE_INFO_1("ERROR=" + ret.ToString(), 10, string.Empty));
                    return ShareInfos.ToArray();
                }
            }
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-RemoteSmbShare", "netview" }; }
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
            get { return "Retrieves the SMB shares of a computer."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List SMB shares of MyServer", "Get-RemoteSmbShare \\MyServer")
                };
            }
        }
    }
}
