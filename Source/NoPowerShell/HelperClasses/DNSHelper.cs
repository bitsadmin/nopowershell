using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    // - Main source: https://www.pinvoke.net/default.aspx/dnsapi.DnsQuery
    // - Struct DNS_RECORD: https://github.com/MichaCo/DnsClient.NET/blob/dev/samples/MiniDig/Interop.DnsQuery.cs
    //     - Because included DNS_RECORD didn't work on x64
    // - Source for switch/case statement: https://www.codeproject.com/Articles/21246/DNS-Query-MFC-based-Application
    public class DnsHelper
    {
        public static readonly Hashtable RecordTypes = new Hashtable(StringComparer.InvariantCultureIgnoreCase)
        {
            { "A", DnsRecordType.DNS_TYPE_A },
            { "NS", DnsRecordType.DNS_TYPE_NS },
            { "MD", DnsRecordType.DNS_TYPE_MD },
            { "MF", DnsRecordType.DNS_TYPE_MF },
            { "CNAME", DnsRecordType.DNS_TYPE_CNAME },
            { "SOA", DnsRecordType.DNS_TYPE_SOA },
            { "MB", DnsRecordType.DNS_TYPE_MB },
            { "MG", DnsRecordType.DNS_TYPE_MG },
            { "MR", DnsRecordType.DNS_TYPE_MR },
            { "WKS", DnsRecordType.DNS_TYPE_WKS },
            { "PTR", DnsRecordType.DNS_TYPE_PTR },
            { "MX", DnsRecordType.DNS_TYPE_MX },
            { "TXT", DnsRecordType.DNS_TYPE_TEXT },
            { "RT", DnsRecordType.DNS_TYPE_RT },
            { "AAAA", DnsRecordType.DNS_TYPE_AAAA },
            { "SRV", DnsRecordType.DNS_TYPE_SRV }
        };

        /// <summary>
        /// Query host based on type
        /// </summary>
        /// <param name="domain">Domain to query</param>
        /// <param name="type">Query type</param>
        /// <returns>List of records</returns>
        public static CommandResult GetRecords(string domain, string type)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new NotSupportedException();
            }

            // Identify if IP address has been entered
            // In that case, perform a reverse lookup
            if (IPAddress.TryParse(domain, out IPAddress ip))
            {
                type = "PTR";

                // IPv6
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    // ipv6.google.com = 2a00:1450:400e:80e::200e -> e.0.0.2.0.0.0.0.0.0.0.0.0.0.0.0.e.0.8.0.e.0.0.4.0.5.4.1.0.0.a.2.ip6.arpa
                    byte[] ipv6_bytes = ip.GetAddressBytes();
                    StringBuilder sb = new StringBuilder(72);
                    for (int i = ipv6_bytes.Length - 1; i >= 0; i--)
                    {
                        byte currentByte = ipv6_bytes[i];
                        byte lo = (byte)(currentByte & 0x0F);
                        byte hi = (byte)(currentByte >> 4);
                        sb.AppendFormat("{0:x}.{1:x}.", lo, hi);
                    }
                    sb.Append("ip6.arpa");
                    domain = sb.ToString();
                }
                // IPv4
                else
                {
                    // Reverse IP address octets
                    string[] octets = ip.ToString().Split('.');
                    Array.Reverse(octets);
                    IPAddress ipreverse = IPAddress.Parse(string.Join(".", octets));

                    // Compile reverse IP address into domain name
                    domain = string.Format("{0}.in-addr.arpa", ipreverse);
                }
            }

            CommandResult results = new CommandResult();
            object foundType = RecordTypes[type];
            if (foundType == null)
            {
                string[] types = new string[RecordTypes.Count];
                RecordTypes.Keys.CopyTo(types, 0);
                throw new NoPowerShellException("Invalid type specified. Specify one of the following: {0}.", string.Join(",", types));
            }
            DnsRecordType queryType = (DnsRecordType)foundType;

            var recordsArray = IntPtr.Zero;
            try
            {
                var result = DnsQuery(ref domain, queryType, DnsQueryOption.DNS_QUERY_BYPASS_CACHE, IntPtr.Zero, ref recordsArray, IntPtr.Zero);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                DNS_RECORD record;
                for (var recordPtr = recordsArray; !recordPtr.Equals(IntPtr.Zero); recordPtr = record.pNext)
                {
                    record = (DNS_RECORD)Marshal.PtrToStructure(recordPtr, typeof(DNS_RECORD));
                    switch (record.wType)
                    {
                        case (ushort)DnsRecordType.DNS_TYPE_A:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "A" },
                                    { "IPAddress", ConvertUintToIpAddressString(record.Data.A.IpAddress) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_NS:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "NS" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.NS.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_MD:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "MD" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.MD.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_MF:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "MF" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.MF.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_CNAME:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "CNAME" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.CNAME.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_SOA:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "SOA" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.SOA.pNamePrimaryServer) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_MB:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "MB" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.MB.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_MG:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "MG" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.MG.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_MR:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "MR" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.MR.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_WKS:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "WKS" },
                                    { "Protocol", ((int)record.Data.WKS.chProtocol).ToString() },
                                    { "Mask", ((int)record.Data.WKS.BitMask).ToString() },
                                    { "IPAddress", ConvertUintToIpAddressString(record.Data.WKS.IpAddress) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_PTR:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "PTR" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.PTR.pNameHost) }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_MX:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "MX" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.MX.pNameExchange) },
                                    { "Preference", record.Data.MX.wPreference.ToString() }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_TEXT:
                            for (uint i = 0; i < record.Data.TXT.dwStringCount; i++)
                            {
                                results.Add(
                                    new ResultRecord()
                                    {
                                        { "Name", Marshal.PtrToStringAuto(record.pName) },
                                        { "Type", "TXT" },
                                        { "Strings", Marshal.PtrToStringAuto(record.Data.TXT.pStringArray) },
                                    }
                                );
                            }
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_RT:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "RT" },
                                    { "NameHost", Marshal.PtrToStringAuto(record.Data.RT.pNameExchange) },
                                    { "Preference", record.Data.RT.wPreference.ToString() }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_AAAA:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "AAAA" },
                                    { "IPAddress", ConvertAAAAToIpAddress(record.Data.AAAA).ToString() }
                                }
                            );
                            break;

                        case (ushort)DnsRecordType.DNS_TYPE_SRV:
                            results.Add(
                                new ResultRecord()
                                {
                                    { "Name", Marshal.PtrToStringAuto(record.pName) },
                                    { "Type", "SRV" },
                                    { "PrimaryServer", Marshal.PtrToStringAuto(record.Data.SRV.pNameTarget) },
                                }
                            );
                            break;

                        default:
                            throw new Exception(string.Format("Unknown: %s type %d", record.pName, record.wType));
                    }
                }

                return results;
            }
            finally
            {
                if (recordsArray != IntPtr.Zero)
                {
                    DnsRecordListFree(recordsArray, DNS_FREE_TYPE.DnsFreeFlat);
                }
            }
        }

        /// <summary>
        /// Provides a DNS query resolution interface
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682016(v=vs.85).aspx
        /// </summary>
        /// <param name="lpstrName">A pointer to a string that represents the DNS name to query</param>
        /// <param name="wType">A value that represents the Resource Record DNS Record Type that is queried</param>
        /// <param name="Options">A value that contains a bitmap of the DNS Query Options to use in the DNS query</param>
        /// <param name="pExtra">Reserved for future use and must be set to NULL</param>
        /// <param name="ppQueryResultsSet">A pointer to a pointer that points to the list of RRs that comprise the response</param>
        /// <param name="pReserved">Reserved for future use and must be set to NULL</param>
        /// <returns>Success (0), or the DNS-specific error defined in Winerror.h</returns>
        [DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, SetLastError = true,
        ExactSpelling = true)]
        public static extern int DnsQuery([MarshalAs(UnmanagedType.VBByRefStr)] ref string lpstrName, DnsRecordType wType,
        DnsQueryOption Options, IntPtr pExtra, ref IntPtr ppQueryResultsSet, IntPtr pReserved);

        /// <summary>
        /// Frees memory allocated for DNS records obtained by using the DnsQuery function
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682021(v=vs.85).aspx
        /// </summary>
        /// <param name="pRecordList">A pointer to the DNS_RECORD structure that contains the list of DNS records to be freed</param>
        /// <param name="FreeType">A specifier of how the record list should be freed</param>
        [DllImport("dnsapi", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void DnsRecordListFree(IntPtr pRecordList, DNS_FREE_TYPE FreeType);

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/cc982162(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum DnsQueryOption
        {
            DNS_QUERY_STANDARD = 0x0,
            DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 0x1,
            DNS_QUERY_USE_TCP_ONLY = 0x2,
            DNS_QUERY_NO_RECURSION = 0x4,
            DNS_QUERY_BYPASS_CACHE = 0x8,
            DNS_QUERY_NO_WIRE_QUERY = 0x10,
            DNS_QUERY_NO_LOCAL_NAME = 0x20,
            DNS_QUERY_NO_HOSTS_FILE = 0x40,
            DNS_QUERY_NO_NETBT = 0x80,
            DNS_QUERY_WIRE_ONLY = 0x100,
            DNS_QUERY_RETURN_MESSAGE = 0x200,
            DNS_QUERY_MULTICAST_ONLY = 0x400,
            DNS_QUERY_NO_MULTICAST = 0x800,
            DNS_QUERY_TREAT_AS_FQDN = 0x1000,
            DNS_QUERY_ADDRCONFIG = 0x2000,
            DNS_QUERY_DUAL_ADDR = 0x4000,
            DNS_QUERY_MULTICAST_WAIT = 0x20000,
            DNS_QUERY_MULTICAST_VERIFY = 0x40000,
            DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
            DNS_QUERY_DISABLE_IDN_ENCODING = 0x200000,
            DNS_QUERY_APPEND_MULTILABEL = 0x800000,
            DNS_QUERY_RESERVED = unchecked((int)0xF0000000)
        }

        /// <summary>
        /// See https://learn.microsoft.com/en-us/windows/win32/dns/dns-constants
        /// Also see http://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml
        /// </summary>
        public enum DnsRecordType
        {
            DNS_TYPE_A = 0x1,
            DNS_TYPE_NS = 0x2,
            DNS_TYPE_MD = 0x3,
            DNS_TYPE_MF = 0x4,
            DNS_TYPE_CNAME = 0x5,
            DNS_TYPE_SOA = 0x6,
            DNS_TYPE_MB = 0x7,
            DNS_TYPE_MG = 0x8,
            DNS_TYPE_MR = 0x9,
            DNS_TYPE_NULL = 0xA,
            DNS_TYPE_WKS = 0xB,
            DNS_TYPE_PTR = 0xC,
            DNS_TYPE_HINFO = 0xD,
            DNS_TYPE_MINFO = 0xE,
            DNS_TYPE_MX = 0xF,
            DNS_TYPE_TEXT = 0x10,
            DNS_TYPE_TXT = DNS_TYPE_TEXT,
            DNS_TYPE_RP = 0x11,
            DNS_TYPE_AFSDB = 0x12,
            DNS_TYPE_X25 = 0x13,
            DNS_TYPE_ISDN = 0x14,
            DNS_TYPE_RT = 0x15,
            DNS_TYPE_NSAP = 0x16,
            DNS_TYPE_NSAPPTR = 0x17,
            DNS_TYPE_SIG = 0x18,
            DNS_TYPE_KEY = 0x19,
            DNS_TYPE_PX = 0x1A,
            DNS_TYPE_GPOS = 0x1B,
            DNS_TYPE_AAAA = 0x1C,
            DNS_TYPE_LOC = 0x1D,
            DNS_TYPE_NXT = 0x1E,
            DNS_TYPE_EID = 0x1F,
            DNS_TYPE_NIMLOC = 0x20,
            DNS_TYPE_SRV = 0x21,
            DNS_TYPE_ATMA = 0x22,
            DNS_TYPE_NAPTR = 0x23,
            DNS_TYPE_KX = 0x24,
            DNS_TYPE_CERT = 0x25,
            DNS_TYPE_A6 = 0x26,
            DNS_TYPE_DNAME = 0x27,
            DNS_TYPE_SINK = 0x28,
            DNS_TYPE_OPT = 0x29,
            DNS_TYPE_DS = 0x2B,
            DNS_TYPE_RRSIG = 0x2E,
            DNS_TYPE_NSEC = 0x2F,
            DNS_TYPE_DNSKEY = 0x30,
            DNS_TYPE_DHCID = 0x31,
            DNS_TYPE_UINFO = 0x64,
            DNS_TYPE_UID = 0x65,
            DNS_TYPE_GID = 0x66,
            DNS_TYPE_UNSPEC = 0x67,
            DNS_TYPE_ADDRS = 0xF8,
            DNS_TYPE_TKEY = 0xF9,
            DNS_TYPE_TSIG = 0xFA,
            DNS_TYPE_IXFR = 0xFB,
            DNS_TYPE_AFXR = 0xFC,
            DNS_TYPE_MAILB = 0xFD,
            DNS_TYPE_MAILA = 0xFE,
            DNS_TYPE_ALL = 0xFF,
            DNS_TYPE_ANY = 0xFF,
            DNS_TYPE_WINS = 0xFF01,
            DNS_TYPE_WINSR = 0xFF02,
            DNS_TYPE_NBSTAT = DNS_TYPE_WINSR
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682056(v=vs.85).aspx
        /// </summary>
        public enum DNS_FREE_TYPE
        {
            DnsFreeFlat = 0,
            DnsFreeRecordList = 1,
            DnsFreeParsedMessageFields = 2
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682082(v=vs.85).aspx
        /// These field offsets could be different depending on endianness and bitness
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_RECORD
        {
            public IntPtr pNext;    // DNS_RECORD*
            public IntPtr pName;    // string
            public ushort wType;
            public ushort wDataLength;
            public FlagsUnion Flags;
            public uint dwTtl;
            public uint dwReserved;
            public DataUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FlagsUnion
        {
            [FieldOffset(0)]
            public uint DW;
            [FieldOffset(0)]
            public DNS_RECORD_FLAGS S;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682084(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_RECORD_FLAGS
        {
            internal uint data;

            // DWORD Section :2;
            public uint Section
            {
                get { return data & 0x3u; }
                set { data = (data & ~0x3u) | (value & 0x3u); }
            }

            // DWORD Delete :1;
            public uint Delete
            {
                get { return (data >> 2) & 0x1u; }
                set { data = (data & ~(0x1u << 2)) | (value & 0x1u) << 2; }
            }

            // DWORD CharSet :2;
            public uint CharSet
            {
                get { return (data >> 3) & 0x3u; }
                set { data = (data & ~(0x3u << 3)) | (value & 0x3u) << 3; }
            }

            // DWORD Unused :3;
            public uint Unused
            {
                get { return (data >> 5) & 0x7u; }
                set { data = (data & ~(0x7u << 5)) | (value & 0x7u) << 5; }
            }

            // DWORD Reserved :24;
            public uint Reserved
            {
                get { return (data >> 8) & 0xFFFFFFu; }
                set { data = (data & ~(0xFFFFFFu << 8)) | (value & 0xFFFFFFu) << 8; }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DataUnion
        {
            [FieldOffset(0)]
            public DNS_A_DATA A;
            [FieldOffset(0)]
            public DNS_SOA_DATA SOA, Soa;
            [FieldOffset(0)]
            public DNS_PTR_DATA PTR, Ptr, NS, Ns, CNAME, Cname, DNAME, Dname, MB, Mb, MD, Md, MF, Mf, MG, Mg, MR, Mr;
            [FieldOffset(0)]
            public DNS_MINFO_DATA MINFO, Minfo, RP, Rp;
            [FieldOffset(0)]
            public DNS_MX_DATA MX, Mx, AFSDB, Afsdb, RT, Rt;
            [FieldOffset(0)]
            public DNS_TXT_DATA HINFO, Hinfo, ISDN, Isdn, TXT, Txt, X25;
            [FieldOffset(0)]
            public DNS_NULL_DATA Null;
            [FieldOffset(0)]
            public DNS_WKS_DATA WKS, Wks;
            [FieldOffset(0)]
            public DNS_AAAA_DATA AAAA;
            [FieldOffset(0)]
            public DNS_KEY_DATA KEY, Key;
            [FieldOffset(0)]
            public DNS_SIG_DATA SIG, Sig;
            [FieldOffset(0)]
            public DNS_ATMA_DATA ATMA, Atma;
            [FieldOffset(0)]
            public DNS_NXT_DATA NXT, Nxt;
            [FieldOffset(0)]
            public DNS_SRV_DATA SRV, Srv;
            [FieldOffset(0)]
            public DNS_NAPTR_DATA NAPTR, Naptr;
            [FieldOffset(0)]
            public DNS_OPT_DATA OPT, Opt;
            [FieldOffset(0)]
            public DNS_DS_DATA DS, Ds;
            [FieldOffset(0)]
            public DNS_RRSIG_DATA RRSIG, Rrsig;
            [FieldOffset(0)]
            public DNS_NSEC_DATA NSEC, Nsec;
            [FieldOffset(0)]
            public DNS_DNSKEY_DATA DNSKEY, Dnskey;
            [FieldOffset(0)]
            public DNS_TKEY_DATA TKEY, Tkey;
            [FieldOffset(0)]
            public DNS_TSIG_DATA TSIG, Tsig;
            [FieldOffset(0)]
            public DNS_WINS_DATA WINS, Wins;
            [FieldOffset(0)]
            public DNS_WINSR_DATA WINSR, WinsR, NBSTAT, Nbstat;
            [FieldOffset(0)]
            public DNS_DHCID_DATA DHCID;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682044(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_A_DATA
        {
            public uint IpAddress;      // IP4_ADDRESS IpAddress;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682096(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_SOA_DATA
        {
            public IntPtr pNamePrimaryServer;       // string
            public IntPtr pNameAdministrator;       // string
            public uint dwSerialNo;
            public uint dwRefresh;
            public uint dwRetry;
            public uint dwExpire;
            public uint dwDefaultTtl;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682080(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_PTR_DATA
        {
            public IntPtr pNameHost;    // string
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682067(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_MINFO_DATA
        {
            public IntPtr pNameMailbox;     // string
            public IntPtr pNameErrorsMailbox;       // string
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682070(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_MX_DATA
        {
            public IntPtr pNameExchange;        // string
            public ushort wPreference;
            public ushort Pad;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682109(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_TXT_DATA
        {
            public uint dwStringCount;
            public IntPtr pStringArray;     // PWSTR pStringArray[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682074(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_NULL_DATA
        {
            public uint dwByteCount;
            public IntPtr Data;           // BYTE  Data[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682120(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_WKS_DATA
        {
            public uint IpAddress;      // IP4_ADDRESS IpAddress;
            public byte chProtocol;     // UCHAR       chProtocol;
            public IntPtr BitMask;        // BYTE    BitMask[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682035(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_AAAA_DATA
        {
            // IP6_ADDRESS Ip6Address;
            // DWORD IP6Dword[4];
            // This isn't ideal, but it should work without using the fixed and unsafe keywords
            public uint Ip6Address0;
            public uint Ip6Address1;
            public uint Ip6Address2;
            public uint Ip6Address3;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682061(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_KEY_DATA
        {
            public ushort wFlags;
            public byte chProtocol;
            public byte chAlgorithm;
            public IntPtr Key;        // BYTE Key[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682094(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_SIG_DATA
        {
            public IntPtr pNameSigner;      // string
            public ushort wTypeCovered;
            public byte chAlgorithm;
            public byte chLabelCount;
            public uint dwOriginalTtl;
            public uint dwExpiration;
            public uint dwTimeSigned;
            public ushort wKeyTag;
            public ushort Pad;
            public IntPtr Signature;      // BYTE  Signature[1];
        }

        public const int DNS_ATMA_MAX_ADDR_LENGTH = 20;
        public const int DNS_ATMA_FORMAT_E164 = 1;
        public const int DNS_ATMA_FORMAT_AESA = 2;

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682041(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_ATMA_DATA
        {
            public byte AddressType;
            // BYTE Address[DNS_ATMA_MAX_ADDR_LENGTH];
            // This isn't ideal, but it should work without using the fixed and unsafe keywords
            public byte Address0;
            public byte Address1;
            public byte Address2;
            public byte Address3;
            public byte Address4;
            public byte Address5;
            public byte Address6;
            public byte Address7;
            public byte Address8;
            public byte Address9;
            public byte Address10;
            public byte Address11;
            public byte Address12;
            public byte Address13;
            public byte Address14;
            public byte Address15;
            public byte Address16;
            public byte Address17;
            public byte Address18;
            public byte Address19;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682076(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_NXT_DATA
        {
            public IntPtr pNameNext;    // string
            public ushort wNumTypes;
            public IntPtr wTypes;       // WORD  wTypes[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682097(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_SRV_DATA
        {
            public IntPtr pNameTarget;      // string
            public ushort uPriority;
            public ushort wWeight;
            public ushort wPort;
            public ushort Pad;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/cc982164(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_NAPTR_DATA
        {
            public ushort wOrder;
            public ushort wPreference;
            public IntPtr pFlags;       // string
            public IntPtr pService;     // string
            public IntPtr pRegularExpression;       // string
            public IntPtr pReplacement;     // string
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd392298(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_OPT_DATA
        {
            public ushort wDataLength;
            public ushort wPad;
            public IntPtr Data;           // BYTE Data[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd392296(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_DS_DATA
        {
            public ushort wKeyTag;
            public byte chAlgorithm;
            public byte chDigestType;
            public ushort wDigestLength;
            public ushort wPad;
            public IntPtr Digest;         // BYTE Digest[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd392301(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_RRSIG_DATA
        {
            public IntPtr pNameSigner;      // string
            public ushort wTypeCovered;
            public byte chAlgorithm;
            public byte chLabelCount;
            public uint dwOriginalTtl;
            public uint dwExpiration;
            public uint dwTimeSigned;
            public ushort wKeyTag;
            public ushort Pad;
            public IntPtr Signature;      // BYTE  Signature[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd392297(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_NSEC_DATA
        {
            public IntPtr pNextDomainName;    // string
            public ushort wTypeBitMapsLength;
            public ushort wPad;
            public IntPtr TypeBitMaps;    // BYTE  TypeBitMaps[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd392295(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_DNSKEY_DATA
        {
            public ushort wFlags;
            public byte chProtocol;
            public byte chAlgorithm;
            public ushort wKeyLength;
            public ushort wPad;
            public IntPtr Key;        // BYTE Key[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682104(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_TKEY_DATA
        {
            public IntPtr pNameAlgorithm;   // string
            public IntPtr pAlgorithmPacket; // PBYTE (which is BYTE*)
            public IntPtr pKey;         // PBYTE (which is BYTE*)
            public IntPtr pOtherData;       // PBYTE (which is BYTE*)
            public uint dwCreateTime;
            public uint dwExpireTime;
            public ushort wMode;
            public ushort wError;
            public ushort wKeyLength;
            public ushort wOtherLength;
            public byte cAlgNameLength;     // UCHAR cAlgNameLength;
            public int bPacketPointers;     // BOOL  bPacketPointers;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682106(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_TSIG_DATA
        {
            public IntPtr pNameAlgorithm;   // string
            public IntPtr pAlgorithmPacket; // PBYTE (which is BYTE*)
            public IntPtr pSignature;       // PBYTE (which is BYTE*)
            public IntPtr pOtherData;       // PBYTE (which is BYTE*)
            public long i64CreateTime;
            public ushort wFudgeTime;
            public ushort wOriginalXid;
            public ushort wError;
            public ushort wSigLength;
            public ushort wOtherLength;
            public byte cAlgNameLength;     // UCHAR    cAlgNameLength;
            public int bPacketPointers;     // BOOL     bPacketPointers;
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682114(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_WINS_DATA
        {
            public uint dwMappingFlag;
            public uint dwLookupTimeout;
            public uint dwCacheTimeout;
            public uint cWinsServerCount;
            public uint WinsServers;    // IP4_ADDRESS WinsServers[1];
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682113(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_WINSR_DATA
        {
            public uint dwMappingFlag;
            public uint dwLookupTimeout;
            public uint dwCacheTimeout;
            public IntPtr pNameResultDomain;    // string
        }

        /// <summary>
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd392294(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DNS_DHCID_DATA
        {
            public uint dwByteCount;
            public IntPtr DHCID;          // BYTE  DHCID[1];
        }

        /// <summary>
        /// Converts an unsigned int to an ip address object
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/cc982163(v=vs.85).aspx
        /// </summary>
        /// <param name="ipAddress">The unsigned int to convert to an ip address object</param>
        /// <returns>The converted ip address</returns>
        public static IPAddress ConvertUintToIpAddress(uint ipAddress)
        {
            // x86 is in little endian
            // Network byte order (what the IPAddress object requires) is big endian
            // Ex - 0x7F000001 is 127.0.0.1
            var addressBytes = new byte[4];
            addressBytes[3] = (byte)((ipAddress & 0xFF000000u) >> 24);
            addressBytes[2] = (byte)((ipAddress & 0x00FF0000u) >> 16);
            addressBytes[1] = (byte)((ipAddress & 0x0000FF00u) >> 8);
            addressBytes[0] = (byte)(ipAddress & 0x000000FFu);
            return new IPAddress(addressBytes);
        }

        public static string ConvertUintToIpAddressString(uint ipAddress)
        {
            return ConvertUintToIpAddress(ipAddress).ToString();
        }

        /// <summary>
        /// Converts the data from the AAAA record into an ip address object
        /// See http://msdn.microsoft.com/en-us/library/windows/desktop/ms682140(v=vs.85).aspx
        /// </summary>
        /// <param name="data">The AAAA record to convert</param>
        /// <returns>The converted ip address</returns>
        public static IPAddress ConvertAAAAToIpAddress(DNS_AAAA_DATA data)
        {
            var addressBytes = new byte[16];
            addressBytes[0] = (byte)(data.Ip6Address0 & 0x000000FF);
            addressBytes[1] = (byte)((data.Ip6Address0 & 0x0000FF00) >> 8);
            addressBytes[2] = (byte)((data.Ip6Address0 & 0x00FF0000) >> 16);
            addressBytes[3] = (byte)((data.Ip6Address0 & 0xFF000000) >> 24);
            addressBytes[4] = (byte)(data.Ip6Address1 & 0x000000FF);
            addressBytes[5] = (byte)((data.Ip6Address1 & 0x0000FF00) >> 8);
            addressBytes[6] = (byte)((data.Ip6Address1 & 0x00FF0000) >> 16);
            addressBytes[7] = (byte)((data.Ip6Address1 & 0xFF000000) >> 24);
            addressBytes[8] = (byte)(data.Ip6Address2 & 0x000000FF);
            addressBytes[9] = (byte)((data.Ip6Address2 & 0x0000FF00) >> 8);
            addressBytes[10] = (byte)((data.Ip6Address2 & 0x00FF0000) >> 16);
            addressBytes[11] = (byte)((data.Ip6Address2 & 0xFF000000) >> 24);
            addressBytes[12] = (byte)(data.Ip6Address3 & 0x000000FF);
            addressBytes[13] = (byte)((data.Ip6Address3 & 0x0000FF00) >> 8);
            addressBytes[14] = (byte)((data.Ip6Address3 & 0x00FF0000) >> 16);
            addressBytes[15] = (byte)((data.Ip6Address3 & 0xFF000000) >> 24);

            return new IPAddress(addressBytes);
        }
    }

    class AdiDnsNode
    {
        public AdiDnsNode(byte[] dnsrecordBytes)
        {
            string dnsValue = string.Empty;

            // Parse dnsRecord
            // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dnsp/6912b338-5472-4f59-b912-0edb536b6ed8
            using (var stream = new MemoryStream(dnsrecordBytes))
            using (var reader = new BinaryReader(stream))
            {
                // Unpack the data using BinaryReader for the first 24 bytes
                ushort dataLength = reader.ReadUInt16();
                RecordType = (DnsHelper.DnsRecordType)reader.ReadUInt16();
                Version = reader.ReadByte();
                Rank = reader.ReadByte();
                Flags = reader.ReadUInt16();
                Serial = reader.ReadUInt32();
                TTL = ReadUint32BigEndian(reader); // TtlSeconds
                uint reserved = reader.ReadUInt32();
                uint ts = reader.ReadUInt32();
                TimeStamp = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime;

                // Parse data element
                byte[] data = reader.ReadBytes(dataLength);
                using (var datastream = new MemoryStream(data))
                using (var datareader = new BinaryReader(datastream))
                {
                    // DNS_RPC_RECORD_A
                    // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dnsp/117c2ff9-9094-45b2-83c2-5e44518e0bac
                    if (RecordType == DnsHelper.DnsRecordType.DNS_TYPE_A)
                    {
                        dnsValue = new IPAddress(datareader.ReadBytes(4)).ToString();
                        RecordData = dnsValue.ToString();
                    }
                    // DNS_RPC_RECORD_SRV
                    // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dnsp/db37cab7-f121-43ba-81c5-ca0e198d4b9a
                    else if (RecordType == DnsHelper.DnsRecordType.DNS_TYPE_SRV)
                    {
                        ushort priority = ReadUInt16BigEndian(datareader);
                        ushort weight = ReadUInt16BigEndian(datareader);
                        ushort port = ReadUInt16BigEndian(datareader);
                        byte length = datareader.ReadByte();
                        byte[] dnsName = new byte[length];
                        datareader.Read(dnsName, 0, (int)length);

                        // Iterate over DNS name parts
                        string dnsNameString;
                        using (var dnsNameStream = new MemoryStream(dnsName))
                        using (var dnsNameReader = new BinaryReader(dnsNameStream))
                        {
                            byte partCount = dnsNameReader.ReadByte();
                            string[] dnsNameParts = new string[partCount];
                            for (byte partIndex = 0; partIndex < partCount; partIndex++)
                            {
                                byte partLength = dnsNameReader.ReadByte();
                                byte[] partBytes = dnsNameReader.ReadBytes(partLength);
                                dnsNameParts[partIndex] = Encoding.ASCII.GetString(partBytes);
                            }

                            dnsNameString = string.Join(".", dnsNameParts);
                        }

                        RecordData = string.Format("[{0}][{1}][{2}][{3}]", priority, weight, port, dnsNameString);
                    }
                    // DNS_TYPE_NS / DNS_RPC_NAME
                    // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dnsp/8f986756-f151-4f5b-bfcf-0d85be8b0d7e
                    // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dnsp/3fd41adc-c69e-407b-979e-721251403132
                    //else if (RecordType == DnsHelper.DnsRecordType.DNS_TYPE_NS)
                    //{
                    //    // TODO
                    //}
                    else
                    {
                        Program.WriteWarning("Record type \"{0}\" is not yet supported. Limited details will be returned.", RecordType.ToString().Replace("DNS_TYPE_",""));
                    }
                }
            }
        }

        private static ushort ReadUInt16BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(2);
            if (bytes.Length < 2)
                throw new EndOfStreamException("Not enough bytes to read a UInt16.");

            // Convert to Big Endian
            Array.Reverse(bytes);

            return BitConverter.ToUInt16(bytes, 0);
        }

        private static uint ReadUint32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (bytes.Length < 4)
                throw new EndOfStreamException("Not enough bytes to read a Uint32.");

            // Convert to Big Endian
            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        public override string ToString()
        {
            return $"{RecordType.ToString().Replace("DNS_TYPE_", "")} {TTL} {RecordData}";
        }

        public DnsHelper.DnsRecordType RecordType { get; set; }
        public ushort Type { get; set; }
        public byte Version { get; set; }
        public byte Rank { get; set; }
        public ushort Flags { get; set; }
        public uint Serial { get; set; }
        public uint TTL { get; set; }
        public DateTime TimeStamp { get; set; }
        public string RecordData { get; set; }
        public bool Tombstoned { get; set; }
    }
}
