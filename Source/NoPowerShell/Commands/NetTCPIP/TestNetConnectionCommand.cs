using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.NetTCPIP
{
    public class TestNetConnectionCommand : PSCommand
    {
        private static readonly byte[] alphabet = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwabcdefghi");

        public TestNetConnectionCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain arguments
            bool performTraceroute = _arguments.Get<BoolArgument>("TraceRoute").Value;
            int count = _arguments.Get<IntegerArgument>("Count").Value;
            int timeout = _arguments.Get<IntegerArgument>("Timeout").Value;
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            int hops = _arguments.Get<IntegerArgument>("Hops").Value;
            int ttl = _arguments.Get<IntegerArgument>("TTL").Value;
            int port = _arguments.Get<IntegerArgument>("Port").Value;

            // Resolve host
            string ip = ResolveIP(computerName);

            // ICMP
            if(port == -1)
            {
                // Traceroute
                if (performTraceroute)
                    _results = PerformTraceroute(ip, computerName, count, timeout, hops);
                // Ping
                else
                    _results = PerformPing(ip, computerName, count, timeout, ttl);
            }
            // TCP port
            else
            {
                _results = PerformPortTest(ip, computerName, port);
            }
            

            return _results;
        }

        private static string ResolveIP(string computerName)
        {
            // If IP is already provided, not required to resolve
            IPAddress ip_addr;
            if (IPAddress.TryParse(computerName, out ip_addr))
                return ip_addr.ToString();

            // In case it is a hostname, resolve it
            IPHostEntry ip = null;
            try
            {
                ip = Dns.GetHostEntry(computerName);
            }
            catch(SocketException)
            {
                throw new NoPowerShellException("Name resolution of {0} failed", computerName);
            }

            return ip.AddressList[0].ToString();
        }

        private static CommandResult PerformPortTest(string ip, string computerName, int port)
        {
            CommandResult results = new CommandResult(1);

            if (port < 1 || port > 65535)
                throw new NoPowerShellException("Cannot validate argument on parameter 'Port'. The {0} argument is greater than the maximum allowed range of 65535. Supply an argument that is less than or equal to 65535 and then try the command again.", port);

            bool connected = false;
            try
            {
                TcpClient client = new TcpClient(ip, port);
                string address = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                string localAddress = ((IPEndPoint)client.Client.LocalEndPoint).Address.ToString();
                connected = true;

                results.Add(new ResultRecord()
                {
                    { "ComputerName", computerName },
                    { "RemoteAddress", address},
                    { "RemotePort", port.ToString() },
                    //{ "InterfaceAlias", string.Empty }, // TODO
                    { "SourceAddress", localAddress },
                    { "TcpTestSucceeded", connected ? "True" : "False" }
                });
            }
            catch(SocketException)
            {
                throw new NoPowerShellException("TCP connect to ({0} : {1}) failed", ip, port);
            }

            return results;
        }

        private static CommandResult PerformPing(string ip, string computerName, int count, int timeout, int ttl)
        {
            CommandResult results = new CommandResult(count);

            Ping ping = new Ping();
            PingOptions options = new PingOptions(ttl, false);

            bool succeeded = false;

            for (int i = 0; i < count; i++)
            {
                PingReply reply = null;

                try
                {
                    reply = ping.Send(ip, timeout, alphabet, options);
                }
                catch(PingException)
                {
                    break;
                }

                succeeded = true;

                // Add to output
                results.Add(
                    new ResultRecord()
                    {
                        { "ComputerName", computerName },
                        { "RemoteAddress", (reply.Address != null) ? reply.Address.ToString() : null },
                        { "PingSucceeded", reply.Status == IPStatus.Success ? "True" : "False" },
                        { "PingReplyDetails (RTT)", reply.RoundtripTime.ToString() }
                    }
                );

                // Send only 1 request per second
                //if (i != count - 1)
                //    Thread.Sleep(1000 - (int)reply.RoundtripTime);
            }

            // Error response
            if (!succeeded)
            {
                results.Add(new ResultRecord()
                {
                    { "ComputerName", computerName },
                    { "RemoteAddress", string.Empty },
                    { "PingSucceeded", succeeded ? "True" : "False" }
                });
            }

            return results;
        }

        private static CommandResult PerformTraceroute(string ip, string computerName, int count, int timeout, int maxHops)
        {
            CommandResult results = new CommandResult(count);

            Ping ping = new Ping();
            List<string> IPs = new List<string>(maxHops);

            // Last hop details
            string remoteAddress = string.Empty;
            bool succeeded = false;
            int rtt = -1;

            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                PingOptions options = new PingOptions(ttl, true);
                PingReply reply = null;

                try
                {
                    reply = ping.Send(ip, timeout, alphabet, options);
                }
                catch(PingException)
                {
                    break;
                }

                if (reply.Status == IPStatus.TtlExpired)
                    IPs.Add(reply.Address.ToString());
                else if (reply.Status == IPStatus.TimedOut)
                    IPs.Add("*");
                else if (reply.Status == IPStatus.Success)
                {
                    IPs.Add(reply.Address.ToString());
                    remoteAddress = reply.Address.ToString();
                    succeeded = true;
                    rtt = (int)reply.RoundtripTime;
                    break;
                }
            }

            ResultRecord record = new ResultRecord()
            {
                { "ComputerName", computerName },
                { "RemoteAddress", remoteAddress },
                { "PingSucceeded", succeeded ? "True" : "False" }
            };

            if(succeeded)
            {
                record.Add("PingReplyDetails (RTT)", rtt.ToString());
                record.Add("TraceRoute", string.Join(", ", IPs.ToArray()));
            }

            results.Add(record);

            return results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get {
                return new CaseInsensitiveList()
                {
                    "Test-NetConnection",
                    "tnc",
                    "ping" // Not official
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("TraceRoute", false),
                    new StringArgument("ComputerName"),
                    new IntegerArgument("Count", 1, true),      // Unofficial parameter
                    new IntegerArgument("Timeout", 5000, true), // Unofficial parameter
                    new IntegerArgument("TTL", 128, true),      // Unofficial parameter
                    new IntegerArgument("Hops", 30, true),
                    new IntegerArgument("Port", -1)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Displays diagnostic information for a connection."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Send ICMP request to host",
                        new List<string>()
                        {
                            "Test-NetConnection 1.1.1.1",
                            "tnc 1.1.1.1"
                        }
                    ),
                    new ExampleEntry("Send 2 ICMP requests to IP address 1.1.1.1 with half a second of timeout", "Test-NetConnection -Count 2 -Timeout 500 1.1.1.1"),
                    new ExampleEntry("Perform a traceroute with a timeout of 1 second and a maximum of 20 hops", "Test-NetConnection -TraceRoute -Timeout 1000 -Hops 20 bitsadm.in"),
                    new ExampleEntry("Perform ping with maximum TTL specified", "ping -TTL 32 1.1.1.1"),
                    new ExampleEntry("Check for open port", "tnc bitsadm.in -Port 80")
                };
            }
        }
    }
}
