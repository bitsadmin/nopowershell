using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Reflection;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADRootDSECommand : PSCommand
    {
        private const string DefaultProperties = "configurationNamingContext, currentTime, defaultNamingContext, dnsHostName, domainControllerFunctionality, domainFunctionality, dsServiceName, forestFunctionality, highestCommittedUSN, isGlobalCatalogReady, isSynchronized, ldapServiceName, namingContexts, rootDomainNamingContext, schemaNamingContext, serverName, subschemaSubentry, supportedCapabilities, supportedControl, supportedLDAPPolicies, supportedLDAPVersion, supportedSASLMechanisms";

        public GetADRootDSECommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute(pipeIn);

            string server = _arguments.Get<StringArgument>("Server").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;
            string propertiesRaw = _arguments.Get<StringArgument>("Properties").Value;

            List<string> properties = ParseProperties(propertiesRaw);

            using (DirectoryEntry rootDse = LDAPHelper.InitializeDirectoryEntry("RootDSE", null, server, username, password))
            {
                try
                {
                    RefreshRootDseCache(rootDse, properties);
                    ResultRecord record = CreateResultRecord(rootDse, properties);

                    if (record != null && record.Count > 0)
                        _results.Add(record);
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    throw new NoPowerShellException($"Unable to query RootDSE: {ex.Message}");
                }
            }

            if (_results.Count == 0)
                Program.WriteWarning("Unable to retrieve RootDSE via LDAP. Check connectivity or specify a reachable domain controller via -Server.");

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-ADRootDSE"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("Server", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            new StringArgument("Properties", DefaultProperties)
        };

        public static new string Synopsis => "Gets the RootDSE directory service entry from Active Directory.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Display the default RootDSE attributes", "Get-ADRootDSE"),
            new ExampleEntry("Query a specific domain controller for all RootDSE attributes", "Get-ADRootDSE -Server dc01.ad.local -Properties *"),
            new ExampleEntry("Request specific RootDSE attributes", "Get-ADRootDSE -Properties defaultNamingContext,configurationNamingContext")
        };

        private static List<string> ParseProperties(string propertiesRaw)
        {
            if (string.IsNullOrWhiteSpace(propertiesRaw))
                return new List<string>(0);

            string[] splitProperties = propertiesRaw.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> properties = new List<string>(splitProperties.Length);

            foreach (string property in splitProperties)
                properties.Add(property.Trim());

            if (properties.Count == 0)
                properties.Add("*");

            return properties;
        }

        private static void RefreshRootDseCache(DirectoryEntry rootDse, List<string> requestedProperties)
        {
            bool selectAll = ShouldSelectAllProperties(requestedProperties);

            if (selectAll || requestedProperties.Count == 0)
            {
                rootDse.RefreshCache();
                return;
            }

            rootDse.RefreshCache(requestedProperties.ToArray());
        }

        private static ResultRecord CreateResultRecord(DirectoryEntry rootDse, List<string> requestedProperties)
        {
            bool selectAll = ShouldSelectAllProperties(requestedProperties);

            if (selectAll)
                return BuildRecordFromAllProperties(rootDse);

            return BuildRecordFromRequestedProperties(rootDse, requestedProperties);
        }

        private static bool ShouldSelectAllProperties(List<string> requestedProperties)
        {
            return requestedProperties.Count == 0 ||
                   (requestedProperties.Count == 1 && requestedProperties[0] == "*");
        }

        private static ResultRecord BuildRecordFromRequestedProperties(DirectoryEntry rootDse, List<string> requestedProperties)
        {
            ResultRecord record = new ResultRecord(requestedProperties.Count);

            foreach (string property in requestedProperties)
                record[property] = string.Empty;

            foreach (string propertyName in rootDse.Properties.PropertyNames)
            {
                if (!record.ContainsKey(propertyName))
                    continue;

                PropertyValueCollection values = rootDse.Properties[propertyName];
                record[propertyName] = FormatPropertyValue(propertyName, values);
            }

            return record;
        }

        private static ResultRecord BuildRecordFromAllProperties(DirectoryEntry rootDse)
        {
            ICollection propertyNames = rootDse.Properties.PropertyNames;
            ResultRecord record = new ResultRecord(propertyNames.Count);

            foreach (string propertyName in propertyNames)
            {
                PropertyValueCollection values = rootDse.Properties[propertyName];
                record[propertyName] = FormatPropertyValue(propertyName, values);
            }

            return record;
        }

        private static string FormatPropertyValue(string propertyName, PropertyValueCollection values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            string[] formattedValues = new string[values.Count];

            for (int i = 0; i < values.Count; i++)
                formattedValues[i] = ConvertSingleValue(propertyName, values[i]);

            return string.Join(";", formattedValues);
        }

        private static string ConvertSingleValue(string propertyName, object value)
        {
            if (value == null)
                return string.Empty;

            if (value is string str)
                return str;

            if (value is DateTime dateTime)
                return dateTime.ToFormattedString();

            if (value is byte[] bytes)
                return BitConverter.ToString(bytes).Replace("-", string.Empty);

            string largeIntegerValue = TryConvertLargeInteger(propertyName, value);
            if (largeIntegerValue != null)
                return largeIntegerValue;

            return value.ToString();
        }

        private static string TryConvertLargeInteger(string propertyName, object value)
        {
            Type valueType = value.GetType();
            if (!string.Equals(valueType.FullName, "System.__ComObject", StringComparison.Ordinal))
                return null;

            try
            {
                int high = (int)valueType.InvokeMember("HighPart", BindingFlags.GetProperty, null, value, null);
                int low = (int)valueType.InvokeMember("LowPart", BindingFlags.GetProperty, null, value, null);
                long combined = ((long)high << 32) + (uint)low;

                if (IsFileTimeAttribute(propertyName) && combined > 0)
                    return DateTime.FromFileTimeUtc(combined).ToFormattedString();

                return combined.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsFileTimeAttribute(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return false;

            switch (propertyName.ToLowerInvariant())
            {
                case "currenttime":
                case "currenttimeserver":
                    return true;
                default:
                    return false;
            }
        }
    }
}
