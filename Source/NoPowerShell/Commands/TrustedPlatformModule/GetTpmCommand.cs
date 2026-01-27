using System;
using System.Management;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.TrustedPlatformModule
{
    public class GetTpmCommand : PSCommand
    {
        public GetTpmCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string computername = _arguments.Get<StringArgument>("ComputerName").Value;

            try
            {
                CommandResult wmiResults = WmiHelper.ExecuteWmiQuery(@"root\CIMV2\Security\MicrosoftTpm", "Select * From Win32_Tpm", computername, null, null);
                if (wmiResults == null || wmiResults.Count == 0)
                {
                    return _results;
                }

                foreach (ResultRecord tpm in wmiResults)
                {
                    ResultRecord record = BuildResultRecord(tpm, computername);
                    _results.Add(record);
                }
            }
            catch (ManagementException mex)
            {
                Program.WriteWarning("Failed to query Win32_Tpm via WMI: {0}", mex.Message);
                _results.Add(new ResultRecord()
                {
                    { string.Empty, "Failed to query TPM information." }
                });
            }
            catch (UnauthorizedAccessException uae)
            {
                Program.WriteWarning("Unauthorized to query Win32_Tpm via WMI: {0}", uae.Message);
                _results.Add(new ResultRecord()
                {
                    { string.Empty, "Unauthorized to query TPM information." }
                });
            }

            return _results;
        }

        private static ResultRecord BuildResultRecord(ResultRecord tpm, string computername)
        {
            ResultRecord record = new ResultRecord()
            {
                { "TpmPresent", "True" },
                { "TpmReady", GetTpmReady(tpm).ToString() },
                { "TpmEnabled", GetValue(tpm, "IsEnabled_InitialValue") },
                { "TpmActivated", GetValue(tpm, "IsActivated_InitialValue") },
                //{ "RestartPending", "" },
                { "ManufacturerId", GetValue(tpm, "ManufacturerID") },
                { "PpiVersion", GetValue(tpm, "PhysicalPresenceVersionInfo") },
                { "ManufacturerIdTxt", GetValue(tpm, "ManufacturerIDTxt") },
                { "ManufacturerVersion", GetValue(tpm, "ManufacturerVersion") },
                { "ManufacturerVersionFull20", GetValue(tpm, "ManufacturerVersionFull20") }
                //{ "ManagedAuthLevel", "" },
                //{ "OwnerAuth", "" },
                //{ "OwnerClearDisabled", "" },
                //{ "AutoProvisioning", "" },
                //{ "LockedOut", "" },
                //{ "LockoutHealTime", "" },
                //{ "LockoutCount", "" },
                //{ "LockoutMax", "" },
                //{ "SelfTest", "" },
            };

            if (ShouldIncludeComputerName(computername))
                record.Add("ComputerName", computername);

            return record;
        }

        private static bool GetTpmReady(ResultRecord tpm)
        {
            bool isActivated = ToBool(tpm, "IsActivated_InitialValue");
            bool isEnabled = ToBool(tpm, "IsEnabled_InitialValue");
            bool isOwned = ToBool(tpm, "IsOwned_InitialValue");

            return isActivated && isEnabled && isOwned;
        }

        private static bool ToBool(ResultRecord record, string key)
        {
            if (record.ContainsKey(key) && bool.TryParse(record[key], out bool value))
                return value;

            return false;
        }

        private static string GetValue(ResultRecord record, string key)
        {
            if (record.ContainsKey(key) && record[key] != null)
                return record[key];

            return string.Empty;
        }

        private static bool ShouldIncludeComputerName(string computername)
        {
            if (string.IsNullOrEmpty(computername))
                return false;

            return !(computername == "." || computername.Equals("localhost", StringComparison.OrdinalIgnoreCase));
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-Tpm"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("ComputerName", true) // Unofficial
        };

        public static new string Synopsis => "Gets Trusted Platform Module (TPM) information via WMI.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Display TPM information on the local computer", "Get-Tpm"),
            new ExampleEntry("Display TPM information remotely using integrated authentication", "Get-Tpm -ComputerName MyServer")
        };
    }
}
