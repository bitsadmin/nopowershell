using System;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.NetSecurity
{
    public class GetNetFirewallRuleCommand : PSCommand
    {
        // https://learn.microsoft.com/en-us/windows/win32/fwp/wmi/wfascimprov/msft-netfirewallrule
        private const string FirewallNamespace = "Root\\StandardCimv2";
        private const string SelectClause = "SELECT InstanceID, DisplayName, Description, DisplayGroup, RuleGroup, Enabled, Profiles, Platforms, Direction, Action, EdgeTraversalPolicy, LooseSourceMapping, LocalOnlyMapping, Owner, PrimaryStatus, Status, EnforcementStatus, PolicyStoreSource, PolicyStoreSourceType, RemoteDynamicKeywordAddresses, PolicyAppId, PackageFamilyName FROM MSFT_NetFirewallRule";

        public GetNetFirewallRuleCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            string displayNameFilter = _arguments.Get<StringArgument>("DisplayName").Value;

            string query = BuildQuery(displayNameFilter);
            _results = WmiHelper.ExecuteWmiQuery(FirewallNamespace, query, computerName, null, null);

            foreach (ResultRecord record in _results)
            {
                // Convert Enabled to boolean string
                if (record.ContainsKey("Enabled"))
                    record["Enabled"] = (record["Enabled"] == "1").ToString();

                // Convert Profiles to profile names
                if (record.ContainsKey("Profiles"))
                {
                    string profileValue = record["Profiles"];
                    if (!string.IsNullOrEmpty(profileValue) && int.TryParse(profileValue, out int profiles))
                    {
                        var profileNames = new System.Collections.Generic.List<string>();
                        if ((profiles & 1) != 0) profileNames.Add("Domain");
                        if ((profiles & 2) != 0) profileNames.Add("Private");
                        if ((profiles & 4) != 0) profileNames.Add("Public");
                        if (profiles == 0) profileNames.Add("Any");

                        record["Profiles"] = profileNames.Count > 0 ? string.Join(", ", profileNames) : profileValue;
                    }
                }

                // Convert Direction to string
                if (record.ContainsKey("Direction"))
                {
                    string directionValue = record["Direction"];
                    if (directionValue == "1")
                        record["Direction"] = "Inbound";
                    else if (directionValue == "2")
                        record["Direction"] = "Outbound";
                }

                // Convert Action to string
                if (record.ContainsKey("Action"))
                {
                    string actionValue = record["Action"];
                    if (actionValue == "2")
                        record["Action"] = "Allow";
                    else if (actionValue == "3")
                        record["Action"] = "AllowBypass";
                    else if (actionValue == "4")
                        record["Action"] = "Block";
                }

                // Convert EdgeTraversalPolicy to string
                if (record.ContainsKey("EdgeTraversalPolicy"))
                {
                    string edgeValue = record["EdgeTraversalPolicy"];
                    if (edgeValue == "0")
                        record["EdgeTraversalPolicy"] = "Block";
                    else if (edgeValue == "1")
                        record["EdgeTraversalPolicy"] = "Allow";
                    else if (edgeValue == "2")
                        record["EdgeTraversalPolicy"] = "DeferToUser";
                    else if (edgeValue == "3")
                        record["EdgeTraversalPolicy"] = "DeferToApp";
                }

                // Convert PrimaryStatus to string
                if (record.ContainsKey("PrimaryStatus"))
                {
                    string statusValue = record["PrimaryStatus"];
                    if (statusValue == "0")
                        record["PrimaryStatus"] = "Unknown";
                    else if (statusValue == "1")
                        record["PrimaryStatus"] = "OK";
                    else if (statusValue == "2")
                        record["PrimaryStatus"] = "Degraded";
                    else if (statusValue == "3")
                        record["PrimaryStatus"] = "Error";
                }

                // Convert EnforcementStatus to string
                if (record.ContainsKey("EnforcementStatus"))
                {
                    string enforcementValue = record["EnforcementStatus"];
                    if (int.TryParse(enforcementValue, out int enforcementInt))
                    {
                        record["EnforcementStatus"] = Enum.GetName(typeof(EnforcementStatus), enforcementInt);
                    }
                }

                // Convert PolicyStoreSourceType to string
                if (record.ContainsKey("PolicyStoreSourceType"))
                {
                    string sourceTypeValue = record["PolicyStoreSourceType"];
                    if (int.TryParse(sourceTypeValue, out int sourceTypeInt))
                    {
                        record["PolicyStoreSourceType"] = Enum.GetName(typeof(PolicyStoreSourcetype), sourceTypeInt);
                    }
                }
            }

            AppendComputerName(_results, computerName);

            return _results;
        }

        private static string BuildQuery(string displayName)
        {
            string query = SelectClause;
            string predicate = BuildDisplayNamePredicate(displayName);

            if (!string.IsNullOrEmpty(predicate))
                query += $" WHERE {predicate}";

            return query;
        }

        private static string BuildDisplayNamePredicate(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            bool containsWildcard = displayName.IndexOf('*') >= 0 || displayName.IndexOf('?') >= 0;
            string sanitized = displayName.Replace("'", "''");

            if (containsWildcard)
            {
                sanitized = sanitized.Replace("*", "%").Replace("?", "_");
                return $"DisplayName LIKE '{sanitized}'";
            }

            return $"DisplayName = '{sanitized}'";
        }

        private static void AppendComputerName(CommandResult results, string computerName)
        {
            if (IsLocalComputer(computerName) || results == null)
                return;

            foreach (ResultRecord record in results)
            {
                if (!record.ContainsKey("ComputerName"))
                    record.Add("ComputerName", computerName);
                else if (string.IsNullOrEmpty(record["ComputerName"]))
                    record["ComputerName"] = computerName;
            }
        }

        private enum EnforcementStatus
        {
            Invalid = 0,
            Full = 1,
            FirewallOffInProfile = 2,
            CategoryOff = 3,
            DisabledObject = 4,
            InactiveProfile = 5,
            LocalAddressResolutionEmpty = 6,
            RemoteAddressResolutionEmpty = 7,
            LocalPortResolutionEmpty = 8,
            RemotePortResolutionEmpty = 9,
            InterfaceResolutionEmpty = 10,
            ApplicationResolutionEmpty = 11,
            RemoteMachineEmpty = 12,
            RemoteUserEmpty = 13,
            LocalGlobalOpenPortsDisallowed = 14,
            LocalAuthorizedApplicationsDisallowed = 15,
            LocalFirewallRulesDisallowed = 16,
            LocalConsecRulesDisallowed = 17,
            NotTargetPlatform = 18,
            OptimizedOut = 19,
            LocalUserEmpty = 20,
            TransportMachinesEmpty = 21,
            TunnelMachinesEmpty = 22,
            TupleResolutionEmpty = 23
        };

        private enum PolicyStoreSourcetype
        {
            None = 0,
            Local = 1,
            GroupPolicy = 2,
            Dynamic = 3,
            Generated = 4,
            Hardcoded = 5,
            MDM = 6,
            HostFirewallLocal = 8,
            HostFirewallGroupPolicy = 9,
            HostFirewallDynamic = 10,
            HostFirewallMDM = 11
        }

        private static bool IsLocalComputer(string computerName)
        {
            return string.IsNullOrWhiteSpace(computerName) ||
                   computerName.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                   computerName.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-NetFirewallRule"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("DisplayName", true),
            new StringArgument("ComputerName", true)
        };

        public static new string Synopsis => "Gets local or remote Windows Firewall rules using WMI (MSFT_NetFirewallRule).";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("List all firewall rules on the local computer", "Get-NetFirewallRule"),
            new ExampleEntry("List only firewall rules whose DisplayName starts with \"Remote Desktop - User Mode\"", "Get-NetFirewallRule -DisplayName \"Remote Desktop - User Mode*\""),
            new ExampleEntry("List firewall rules on a remote computer over WMI", "Get-NetFirewallRule -ComputerName MyServer")
        };
    }
}
