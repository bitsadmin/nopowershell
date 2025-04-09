using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Security;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetWinEventCommand : PSCommand
    {
        // Inspired by https://github.com/PowerShell/PowerShell/blob/master/src/Microsoft.PowerShell.Commands.Diagnostics/GetEventCommand.cs
        public GetWinEventCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect the (optional) ComputerName, Username and Password parameters
            base.Execute();
            //if (computername.ToLowerInvariant() == ".")
            //    computername = string.Empty;

            // Obtain cmdlet parameters
            string logName = _arguments.Get<StringArgument>("LogName").Value;
            string filterXpath = _arguments.Get<StringArgument>("FilterXPath").Value;
            int maxEvents = _arguments.Get<IntegerArgument>("MaxEvents").Value;
            bool oldest = _arguments.Get<BoolArgument>("Oldest").Value;

            // Validate existence of log
            try
            {
                if (!EventLog.Exists(logName, computername))
                    throw new NoPowerShellException("There is not an event log on the {0} computer that matches \"{1}\".", computername, logName);
            }
            catch (SecurityException ex)
            {
                throw new NoPowerShellException("To access the '{0}' log start NoPowerShell with elevated user rights. Error: {1}.", logName, ex.Message);
            }

            // Username and password are set
            EventLogSession eventLogSession = null;
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                // Obtain domain from user
                string domain = ".";
                if (username.Contains("\\"))
                {
                    string[] split = username.Split(new char[] { '\\' }, 2);
                    domain = split[0];
                    username = split[1];
                }

                // Convert password to secure string
                SecureString securePassword = new SecureString();
                if (!string.IsNullOrEmpty(password))
                {
                    foreach (char c in password)
                    {
                        securePassword.AppendChar(c);
                    }
                }

                eventLogSession = new EventLogSession(computername, domain, username, securePassword, SessionAuthentication.Negotiate);
            }
            // No credentials provided
            else
            {
                eventLogSession = new EventLogSession(computername);
            }

            // Perform query
            using (eventLogSession)
            {
                EventLogQuery logQuery = new EventLogQuery(logName, PathType.LogName, filterXpath) { ReverseDirection = !oldest };
                EventLogReader reader = new EventLogReader(logQuery);

                long numEvents = 0;
                while (true)
                {
                    EventRecord eventRecord = reader.ReadEvent();

                    // Stop if end
                    if (eventRecord == null)
                        break;

                    // Stop if MaxEvents is specified & reached
                    if (maxEvents != -1 && numEvents >= maxEvents)
                        break;

                    string timeCreated = eventRecord.TimeCreated.HasValue ? eventRecord.TimeCreated.Value.ToFormattedString() : "N/A";
                    string id = eventRecord.Id.ToString();
                    string levelDisplayName;
                    try
                    {
                        levelDisplayName = eventRecord.LevelDisplayName;
                    }
                    catch(Exception ex)
                    {
                        levelDisplayName = string.Format("Exception: {0}", ex.Message);
                    }
                    string message = string.Empty;
                    try
                    {
                        message = eventRecord.FormatDescription();
                    }
                    catch(Exception ex)
                    {
                        levelDisplayName = string.Format("Exception: {0}", ex.Message);
                    }

                    _results.Add(
                        new ResultRecord()
                        {
                            { "TimeCreated", timeCreated },
                            { "Id", id },
                            { "LevelDisplayName", levelDisplayName },
                            { "Message", message }
                        }
                    );

                    numEvents++;
                }
            }

            // Return results
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-WinEvent" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("LogName"),
                    new StringArgument("FilterXPath", "*"),
                    new IntegerArgument("MaxEvents", 100, true),
                    new BoolArgument("Oldest")
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets events from event logs and event tracing log files on local and remote computers."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List oldest 10 events of Application log", "Get-WinEvent -LogName Application -MaxEvents 10 -Oldest"),
                    new ExampleEntry("Determine the IP address from where a specific user is authenticating to the DC", "Get-WinEvent -LogName Security -FilterXPath \"*[System[(EventID=4624)]] and *[EventData[Data[@Name='TargetUserName']='bitsadmin']]\" -ComputerName DC1 -MaxEvents 1"),
                };
            }
        }
    }
}
