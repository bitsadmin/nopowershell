using System;
using System.Collections.Generic;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.NetSecurity
{
    public class GetNetFirewallProfileCommand : PSCommand
    {
        // https://learn.microsoft.com/en-us/windows/win32/fwp/wmi/wfascimprov/msft-netfirewallprofile
        private const string FirewallNamespace = "Root\\StandardCimv2";
        private const string SelectClause = "SELECT Name, Enabled, DefaultInboundAction, DefaultOutboundAction, AllowInboundRules, AllowLocalFirewallRules, AllowLocalIPsecRules, AllowUserApps, AllowUserPorts, AllowUnicastResponseToMulticast, NotifyOnListen, EnableStealthModeForIPsec, LogFileName, LogMaxSizeKilobytes, LogAllowed, LogBlocked, LogIgnored, DisabledInterfaceAliases FROM MSFT_NetFirewallProfile";

        public GetNetFirewallProfileCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string nameFilter = _arguments.Get<StringArgument>("Name").Value;
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;

            string query = BuildQuery(nameFilter);
            _results = WmiHelper.ExecuteWmiQuery(FirewallNamespace, query, computerName, null, null);

            NormalizeResults(_results);
            AppendComputerName(_results, computerName);

            return _results;
        }

        private static string BuildQuery(string nameFilter)
        {
            string query = SelectClause;
            string predicate = BuildNamePredicate(nameFilter);

            if (!string.IsNullOrEmpty(predicate))
                query += $" WHERE {predicate}";

            return query;
        }

        private static string BuildNamePredicate(string nameFilter)
        {
            if (string.IsNullOrWhiteSpace(nameFilter))
                return null;

            string[] rawNames = nameFilter.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (rawNames.Length == 0)
                return null;

            List<string> predicates = new List<string>(rawNames.Length);
            foreach (string rawName in rawNames)
            {
                string trimmed = rawName.Trim();
                if (trimmed.Length == 0)
                    continue;

                bool containsWildcard = trimmed.IndexOf('*') >= 0 || trimmed.IndexOf('?') >= 0;
                string sanitized = trimmed.Replace("'", "''");

                if (containsWildcard)
                {
                    sanitized = sanitized.Replace("*", "%").Replace("?", "_");
                    predicates.Add($"Name LIKE '{sanitized}'");
                }
                else
                {
                    predicates.Add($"Name = '{sanitized}'");
                }
            }

            if (predicates.Count == 0)
                return null;

            if (predicates.Count == 1)
                return predicates[0];

            return $"({string.Join(" OR ", predicates)})";
        }

        private static void NormalizeResults(CommandResult results)
        {
            if (results is null)
                return;

            foreach (ResultRecord record in results)
            {
                ConvertBoolean(record, "Enabled");
                ConvertAction(record, "DefaultInboundAction");
                ConvertAction(record, "DefaultOutboundAction");
                ConvertProfileSetting(record, "AllowInboundRules");
                ConvertProfileSetting(record, "AllowLocalFirewallRules");
                ConvertProfileSetting(record, "AllowLocalIPsecRules");
                ConvertProfileSetting(record, "AllowUserApps");
                ConvertProfileSetting(record, "AllowUserPorts");
                ConvertProfileSetting(record, "AllowUnicastResponseToMulticast");
                ConvertBoolean(record, "NotifyOnListen");
                ConvertProfileSetting(record, "EnableStealthModeForIPsec");
                ConvertBoolean(record, "LogAllowed");
                ConvertBoolean(record, "LogBlocked");
                ConvertProfileSetting(record, "LogIgnored");
                NormalizeDisabledInterfaceAliases(record);
            }
        }

        private static void ConvertBoolean(ResultRecord record, string key)
        {
            if (!record.ContainsKey(key))
                return;

            string value = record[key];
            if (string.IsNullOrEmpty(value))
                return;

            if (int.TryParse(value, out int numeric))
            {
                record[key] = (numeric != 0).ToString();
                return;
            }

            if (bool.TryParse(value, out bool boolValue))
                record[key] = boolValue.ToString();
        }

        private static void ConvertAction(ResultRecord record, string key)
        {
            if (!record.ContainsKey(key))
                return;

            string value = record[key];
            if (!int.TryParse(value, out int numeric))
                return;

            record[key] = ConvertActionValue(numeric);
        }

        private static void ConvertProfileSetting(ResultRecord record, string key)
        {
            if (!record.ContainsKey(key))
                return;

            string value = record[key];
            if (!int.TryParse(value, out int numeric))
                return;

            record[key] = ConvertProfileSettingValue(numeric);
        }

        private static void NormalizeDisabledInterfaceAliases(ResultRecord record)
        {
            const string key = "DisabledInterfaceAliases";
            if (!record.ContainsKey(key))
                return;

            string value = record[key];
            record[key] = string.IsNullOrWhiteSpace(value) ? "{NotConfigured}" : $"{{{value}}}";
        }

        private static void AppendComputerName(CommandResult results, string computerName)
        {
            if (IsLocalComputer(computerName) || results is null)
                return;

            foreach (ResultRecord record in results)
            {
                if (!record.ContainsKey("ComputerName"))
                    record.Add("ComputerName", computerName);
                else if (string.IsNullOrEmpty(record["ComputerName"]))
                    record["ComputerName"] = computerName;
            }
        }

        private static bool IsLocalComputer(string computerName)
        {
            return string.IsNullOrWhiteSpace(computerName) ||
                   computerName.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                   computerName.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }

        private static string ConvertActionValue(int value)
        {
            switch (value)
            {
                case 0:
                    return "NotConfigured";
                case 2:
                    return "Allow";
                case 4:
                    return "Block";
                default:
                    return value.ToString();
            }
        }

        private static string ConvertProfileSettingValue(int value)
        {
            switch (value)
            {
                case 0:
                    return "False";
                case 1:
                    return "True";
                case 2:
                    return "NotConfigured";
                default:
                    return value.ToString();
            }
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-NetFirewallProfile"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Name", true),
            new StringArgument("ComputerName", true)
        };

        public static new string Synopsis => "Gets local or remote Windows Firewall profiles via WMI (MSFT_NetFirewallProfile).";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("List all firewall profiles", "Get-NetFirewallProfile"),
            new ExampleEntry("List only the Public firewall profile", "Get-NetFirewallProfile -Name Public")
        };
    }
}
