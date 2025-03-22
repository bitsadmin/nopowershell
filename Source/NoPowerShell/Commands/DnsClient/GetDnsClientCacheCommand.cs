using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetDnsClientCacheCommand : PSCommand
    {
        public GetDnsClientCacheCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters for remote execution
            base.Execute();

            CommandResult dnsClientCache = WmiHelper.ExecuteWmiQuery("root/StandardCimv2", "Select Entry, Name, Type, Status, Section, TimeToLive, DataLength, Data From MSFT_DNSClientCache", computername, username, password);
            foreach (ResultRecord entry in dnsClientCache)
            {
                DnsHelper.DnsRecordType recordType = (DnsHelper.DnsRecordType)Convert.ToInt32(entry["Type"]);
                int status = Convert.ToInt32(entry["Status"]);
                string strStatus;
                switch(status)
                {
                    case 0:
                        strStatus = "Success";
                        break;
                    case 9003:
                        strStatus = "NotExist";
                        break;
                    case 9701:
                        strStatus = "NoRecords";
                        break;
                    default:
                        strStatus = "Unknown";
                        break;
                }

                _results.Add(
                    new ResultRecord()
                    {
                        { "Entry", entry["Entry"] },
                        { "RecordName", entry["Name"] },
                        { "RecordType", recordType.ToString().Replace("DNS_TYPE_","") },
                        { "Status", strStatus },
                        { "Section", entry["Section"] },
                        { "TimeToLive", entry["TimeToLive"] },
                        { "DataLength", entry["DataLength"] },
                        { "Data", entry["Data"] }
                    }
                );
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-DnsClientCache" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Retrieves the contents of the DNS client cache."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List cached DNS entries on the local computer", "Get-DnsClientCache"),
                    new ExampleEntry
                    (
                        "List cached DNS entries from a remote computer using WMI",
                        new List<string>()
                        {
                            "Get-DnsClientCache -ComputerName MyServer -Username Administrator -Password Pa$$w0rd",
                            "Get-DnsClientCache -ComputerName MyServer"
                        }
                    )
                };
            }
        }
    }
}
