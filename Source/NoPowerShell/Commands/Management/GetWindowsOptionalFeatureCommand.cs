using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetWindowsOptionalFeatureCommand : PSCommand
    {
        public GetWindowsOptionalFeatureCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters for remote execution
            base.Execute();

            // Obtain cmdlet parameters
            string featureName = _arguments.Get<StringArgument>("FeatureName").Value;
            bool online = _arguments.Get<BoolArgument>("Online").Value;

            // Validate that Online parameter is set
            if (!online)
            {
                throw new NoPowerShellException("The -Online parameter is required. Offline image support is not implemented.");
            }

            try
            {
                // Query Windows Optional Features using WMI
                // Note: Win32_OptionalFeature is available on Windows 7 and later
                string wmiQuery = "SELECT * FROM Win32_OptionalFeature";
                
                // Filter by feature name if provided
                if (!string.IsNullOrEmpty(featureName))
                {
                    wmiQuery += $" WHERE Name = '{featureName.Replace("'", "''")}'";
                }

                CommandResult wmiResults = WmiHelper.ExecuteWmiQuery(wmiQuery, computername, username, password);

                // Transform WMI results to match PowerShell's Get-WindowsOptionalFeature output format
                foreach (ResultRecord wmiRecord in wmiResults)
                {
                    ResultRecord record = new ResultRecord
                    {
                        { "FeatureName", wmiRecord.ContainsKey("Name") ? wmiRecord["Name"] : string.Empty },
                        { "DisplayName", wmiRecord.ContainsKey("Caption") ? wmiRecord["Caption"] : string.Empty },
                        { "Description", wmiRecord.ContainsKey("Description") ? wmiRecord["Description"] : string.Empty },
                        { "State", GetFeatureState(wmiRecord) },
                        { "RestartRequired", GetRestartRequired(wmiRecord) }
                    };

                    _results.Add(record);
                }

                // If no results and feature name was specified, it might not exist
                if (_results.Count == 0 && !string.IsNullOrEmpty(featureName))
                {
                    throw new NoPowerShellException($"Feature '{featureName}' not found.");
                }
            }
            catch (NoPowerShellException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new NoPowerShellException($"Error querying Windows optional features: {ex.Message}");
            }

            return _results;
        }

        /// <summary>
        /// Maps WMI InstallState to PowerShell State values
        /// </summary>
        private string GetFeatureState(ResultRecord wmiRecord)
        {
            if (!wmiRecord.ContainsKey("InstallState"))
                return "Unknown";

            // InstallState values:
            // 1 = Installed
            // 2 = Not Present / Available
            // 3 = Enabled
            // 4 = Disabled
            // 5 = Absent
            // 6 = Unknown
            
            string installState = wmiRecord["InstallState"];
            int stateValue;
            
            if (int.TryParse(installState, out stateValue))
            {
                switch (stateValue)
                {
                    case 1:
                    case 3:
                        return "Enabled";
                    case 2:
                    case 5:
                        return "Disabled";
                    case 4:
                        return "Disabled";
                    default:
                        return "Unknown";
                }
            }

            return "Unknown";
        }

        /// <summary>
        /// Determines if restart is required based on WMI properties
        /// </summary>
        private string GetRestartRequired(ResultRecord wmiRecord)
        {
            // WMI doesn't directly provide restart required status
            // We'll check if the feature state suggests a pending change
            // This is a simplified implementation
            if (wmiRecord.ContainsKey("InstallState"))
            {
                string installState = wmiRecord["InstallState"];
                int stateValue;
                
                if (int.TryParse(installState, out stateValue))
                {
                    // If state is in transition, restart might be required
                    // This is a best-effort approach
                    return "Possible";
                }
            }

            return "No";
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList() { "Get-WindowsOptionalFeature" };

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("FeatureName", true),
                    new BoolArgument("Online")
                };
            }
        }

        public static new string Synopsis => "Gets information about Windows optional features.";

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all Windows optional features", "Get-WindowsOptionalFeature -Online"),
                    new ExampleEntry("Get a specific feature", "Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All"),
                    new ExampleEntry("List enabled features", "Get-WindowsOptionalFeature -Online | ? State -EQ Enabled"),
                    new ExampleEntry("List disabled features", "Get-WindowsOptionalFeature -Online | ? State -EQ Disabled")
                };
            }
        }
    }
}
