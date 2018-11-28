using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class TestNetConnectionCommand : PSCommand
    {
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

            // Traceroute or Ping
            if (performTraceroute)
                _results = PerformTraceroute(computerName, count, timeout, hops);
            else
                _results = PerformPing(computerName, count, timeout);

            return _results;
        }

        private static CommandResult PerformPing(string computerName, int count, int timeout)
        {
            CommandResult results = new CommandResult(count);

            Ping ping = new Ping();
            PingOptions options = new PingOptions(64, false);

            bool succeeded = false;

            for (int i = 0; i < count; i++)
            {
                PingReply reply = null;

                try
                {
                    reply = ping.Send(computerName, timeout);
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
                        { "RemoteAddress", reply.Address.ToString() },
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

        private static CommandResult PerformTraceroute(string computerName, int count, int timeout, int maxHops)
        {
            CommandResult results = new CommandResult(count);

            // Fill buffer with a-z
            byte[] buffer = new byte[32];
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Convert.ToByte(0x61 + (i % 26));

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
                    reply = ping.Send(computerName, timeout, buffer, options);
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
            get { return new CaseInsensitiveList() { "Test-NetConnection", "ping" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new BoolArgument("TraceRoute", false),
                    new StringArgument("ComputerName"),
                    new IntegerArgument("Count", 1, true),
                    new IntegerArgument("Timeout", 5000, true),
                    new IntegerArgument("Hops", 30, true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Displays diagnostic information for a connection."; }
        }
    }
}
