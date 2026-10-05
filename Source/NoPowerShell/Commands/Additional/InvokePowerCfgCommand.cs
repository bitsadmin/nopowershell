using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class InvokePowerCfgCommand : PSCommand
    {
        public InvokePowerCfgCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            bool list = _arguments.Get<BoolArgument>("List").Value;
            bool query = _arguments.Get<BoolArgument>("Query").Value;
            StringArgument querySchemeGuidArgument = _arguments.Get<StringArgument>("QuerySchemeGuid");
            StringArgument querySubgroupGuidArgument = _arguments.Get<StringArgument>("QuerySubgroupGuid");
            StringArgument setActiveArgument = _arguments.Get<StringArgument>("SetActive");
            IntegerArgument setHibernateTimeoutAcArgument = _arguments.Get<IntegerArgument>("SetHibernateTimeoutAc");
            IntegerArgument setHibernateTimeoutDcArgument = _arguments.Get<IntegerArgument>("SetHibernateTimeoutDc");
            IntegerArgument setStandbyTimeoutAcArgument = _arguments.Get<IntegerArgument>("SetStandbyTimeoutAc");
            IntegerArgument setStandbyTimeoutDcArgument = _arguments.Get<IntegerArgument>("SetStandbyTimeoutDc");
            bool disableLidSleep = _arguments.Get<BoolArgument>("DisableLidSleep").Value;
            bool enableLidSleep = _arguments.Get<BoolArgument>("EnableLidSleep").Value;
            bool reactivateCurrentScheme = _arguments.Get<BoolArgument>("ReactivateCurrentScheme").Value;

            ValidateArguments(
                list,
                query,
                querySchemeGuidArgument,
                querySubgroupGuidArgument,
                setActiveArgument,
                setHibernateTimeoutAcArgument,
                setHibernateTimeoutDcArgument,
                setStandbyTimeoutAcArgument,
                setStandbyTimeoutDcArgument,
                disableLidSleep,
                enableLidSleep,
                reactivateCurrentScheme
            );

            if (list)
            {
                ListPowerSchemes();
            }

            if (query)
            {
                QueryPowerSettings(querySchemeGuidArgument, querySubgroupGuidArgument);
            }

            if (setActiveArgument.IsSet)
            {
                Guid setActiveScheme = ResolveSchemeIdentifier(setActiveArgument.Value);
                SetActiveScheme("SetActive", setActiveScheme);
            }

            if (setHibernateTimeoutAcArgument.IsSet)
            {
                WritePowerValue("SetHibernateTimeoutAc", true, GUID_SLEEP_SUBGROUP, GUID_HIBERNATE_TIMEOUT, (uint)setHibernateTimeoutAcArgument.Value);
            }

            if (setHibernateTimeoutDcArgument.IsSet)
            {
                WritePowerValue("SetHibernateTimeoutDc", false, GUID_SLEEP_SUBGROUP, GUID_HIBERNATE_TIMEOUT, (uint)setHibernateTimeoutDcArgument.Value);
            }

            if (setStandbyTimeoutAcArgument.IsSet)
            {
                WritePowerValue("SetStandbyTimeoutAc", true, GUID_SLEEP_SUBGROUP, GUID_STANDBY_TIMEOUT, (uint)setStandbyTimeoutAcArgument.Value);
            }

            if (setStandbyTimeoutDcArgument.IsSet)
            {
                WritePowerValue("SetStandbyTimeoutDc", false, GUID_SLEEP_SUBGROUP, GUID_STANDBY_TIMEOUT, (uint)setStandbyTimeoutDcArgument.Value);
            }

            if (disableLidSleep)
            {
                WritePowerValue("DisableLidSleepAc", true, GUID_BUTTONS_SUBGROUP, GUID_LID_ACTION, 0);
                WritePowerValue("DisableLidSleepDc", false, GUID_BUTTONS_SUBGROUP, GUID_LID_ACTION, 0);
            }

            if (enableLidSleep)
            {
                WritePowerValue("EnableLidSleepAc", true, GUID_BUTTONS_SUBGROUP, GUID_LID_ACTION, 1);
                WritePowerValue("EnableLidSleepDc", false, GUID_BUTTONS_SUBGROUP, GUID_LID_ACTION, 1);
            }

            if (reactivateCurrentScheme)
            {
                Guid currentScheme = GetActiveSchemeGuid();
                SetActiveScheme("ReactivateCurrentScheme", currentScheme);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Invoke-PowerCfg",
            "powercfg"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new BoolArgument("List"),
            new BoolArgument("Query"),
            new StringArgument("QuerySchemeGuid", true),
            new StringArgument("QuerySubgroupGuid", true),
            new StringArgument("SetActive", true),
            new IntegerArgument("SetHibernateTimeoutAc", 0),
            new IntegerArgument("SetHibernateTimeoutDc", 0),
            new IntegerArgument("SetStandbyTimeoutAc", 0),
            new IntegerArgument("SetStandbyTimeoutDc", 0),
            new BoolArgument("DisableLidSleep"),
            new BoolArgument("EnableLidSleep"),
            new BoolArgument("ReactivateCurrentScheme")
        };

        public static new string Synopsis => "Manage power schemes and query power settings using native Windows power APIs.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("List all available power schemes", "Invoke-PowerCfg -List"),
            new ExampleEntry("Query all settings of the active scheme", "Invoke-PowerCfg -Query"),
            new ExampleEntry("Query all settings of a specific scheme", "Invoke-PowerCfg -Query 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
            new ExampleEntry("Query all settings of a subgroup in a specific scheme", "Invoke-PowerCfg -Query 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 238c9fa8-0aad-41ed-83f4-97be242c8f20"),
            new ExampleEntry("Set active scheme to high performance", "Invoke-PowerCfg -SetActive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
            new ExampleEntry("Disable hibernate timeout", "Invoke-PowerCfg -SetHibernateTimeoutAc 0 -SetHibernateTimeoutDc 0"),
            new ExampleEntry("Disable standby timeout", "Invoke-PowerCfg -SetStandbyTimeoutAc 0 -SetStandbyTimeoutDc 0"),
            new ExampleEntry("Disable sleep when closing lid", "Invoke-PowerCfg -DisableLidSleep"),
            new ExampleEntry("Enable sleep when closing lid", "Invoke-PowerCfg -EnableLidSleep"),
            new ExampleEntry("Re-activate current scheme", "Invoke-PowerCfg -ReactivateCurrentScheme")
        };

        private static void ValidateArguments(
            bool list,
            bool query,
            StringArgument querySchemeGuidArgument,
            StringArgument querySubgroupGuidArgument,
            StringArgument setActiveArgument,
            IntegerArgument setHibernateTimeoutAcArgument,
            IntegerArgument setHibernateTimeoutDcArgument,
            IntegerArgument setStandbyTimeoutAcArgument,
            IntegerArgument setStandbyTimeoutDcArgument,
            bool disableLidSleep,
            bool enableLidSleep,
            bool reactivateCurrentScheme)
        {
            if (disableLidSleep && enableLidSleep)
                throw new NoPowerShellException("DisableLidSleep and EnableLidSleep cannot be used together.");

            if (setActiveArgument.IsSet && string.IsNullOrWhiteSpace(setActiveArgument.Value))
                throw new NoPowerShellException("SetActive must contain a scheme identifier.");

            if ((querySchemeGuidArgument.IsSet || querySubgroupGuidArgument.IsSet) && !query)
                throw new NoPowerShellException("QuerySchemeGuid and QuerySubgroupGuid require -Query.");

            if (querySubgroupGuidArgument.IsSet && !querySchemeGuidArgument.IsSet)
                throw new NoPowerShellException("QuerySubgroupGuid cannot be provided without QuerySchemeGuid.");

            if (querySchemeGuidArgument.IsSet)
            {
                Guid schemeGuid;
                if (!Guid.TryParse(querySchemeGuidArgument.Value, out schemeGuid))
                    throw new NoPowerShellException("QuerySchemeGuid must be a valid GUID.");
            }

            if (querySubgroupGuidArgument.IsSet)
            {
                Guid subGroupGuid;
                if (!Guid.TryParse(querySubgroupGuidArgument.Value, out subGroupGuid))
                    throw new NoPowerShellException("QuerySubgroupGuid must be a valid GUID.");
            }

            bool hasMutationAction =
                list ||
                setActiveArgument.IsSet ||
                setHibernateTimeoutAcArgument.IsSet ||
                setHibernateTimeoutDcArgument.IsSet ||
                setStandbyTimeoutAcArgument.IsSet ||
                setStandbyTimeoutDcArgument.IsSet ||
                disableLidSleep ||
                enableLidSleep ||
                reactivateCurrentScheme;

            if (query && hasMutationAction)
                throw new NoPowerShellException("Query cannot be combined with other action parameters.");

            ValidateNonNegativeTimeout(setHibernateTimeoutAcArgument, "SetHibernateTimeoutAc");
            ValidateNonNegativeTimeout(setHibernateTimeoutDcArgument, "SetHibernateTimeoutDc");
            ValidateNonNegativeTimeout(setStandbyTimeoutAcArgument, "SetStandbyTimeoutAc");
            ValidateNonNegativeTimeout(setStandbyTimeoutDcArgument, "SetStandbyTimeoutDc");

            bool hasAction =
                list ||
                query ||
                setActiveArgument.IsSet ||
                setHibernateTimeoutAcArgument.IsSet ||
                setHibernateTimeoutDcArgument.IsSet ||
                setStandbyTimeoutAcArgument.IsSet ||
                setStandbyTimeoutDcArgument.IsSet ||
                disableLidSleep ||
                enableLidSleep ||
                reactivateCurrentScheme;

            if (!hasAction)
                throw new NoPowerShellException("Specify at least one action parameter.");
        }

        private void ListPowerSchemes()
        {
            Guid activeSchemeGuid = GetActiveSchemeGuid();
            uint index = 0;

            while (true)
            {
                uint guidSize = (uint)Marshal.SizeOf(typeof(Guid));
                IntPtr schemeGuidPointer = Marshal.AllocHGlobal((int)guidSize);

                try
                {
                    uint status = NativeMethods.PowerEnumerate(
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        ACCESS_SCHEME,
                        index,
                        schemeGuidPointer,
                        ref guidSize
                    );

                    if (status == ERROR_NO_MORE_ITEMS)
                        break;

                    EnsureSuccess(status, "PowerEnumerate");

                    Guid schemeGuid = (Guid)Marshal.PtrToStructure(schemeGuidPointer, typeof(Guid));
                    string schemeName = ReadSchemeFriendlyName(schemeGuid);

                    _results.Add(new ResultRecord()
                    {
                        { "SchemeGuid", schemeGuid.ToString() },
                        { "SchemeName", schemeName },
                        { "IsActive", schemeGuid.Equals(activeSchemeGuid).ToString() }
                    });

                    index++;
                }
                finally
                {
                    Marshal.FreeHGlobal(schemeGuidPointer);
                }
            }
        }

        private static string ReadSchemeFriendlyName(Guid schemeGuid)
        {
            uint bufferSize = 0;
            uint status = NativeMethods.PowerReadFriendlyName(
                IntPtr.Zero,
                ref schemeGuid,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                ref bufferSize
            );

            if (status != 0 && status != ERROR_MORE_DATA)
                EnsureSuccess(status, "PowerReadFriendlyName");

            if (bufferSize == 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = NativeMethods.PowerReadFriendlyName(
                    IntPtr.Zero,
                    ref schemeGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    buffer,
                    ref bufferSize
                );
                EnsureSuccess(status, "PowerReadFriendlyName");

                byte[] rawName = new byte[bufferSize];
                Marshal.Copy(buffer, rawName, 0, (int)bufferSize);
                return Encoding.Unicode.GetString(rawName).TrimEnd('\0');
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void QueryPowerSettings(StringArgument querySchemeGuidArgument, StringArgument querySubgroupGuidArgument)
        {
            Guid schemeGuid = querySchemeGuidArgument.IsSet
                ? ParseGuidArgument(querySchemeGuidArgument.Value, "QuerySchemeGuid")
                : GetActiveSchemeGuid();

            Guid? subgroupGuid = null;
            if (querySubgroupGuidArgument.IsSet)
                subgroupGuid = ParseGuidArgument(querySubgroupGuidArgument.Value, "QuerySubgroupGuid");

            QueryPowerSettings(schemeGuid, subgroupGuid);
        }

        private void QueryPowerSettings(Guid schemeGuid, Guid? subgroupFilter)
        {
            string schemeName = ReadSchemeFriendlyName(schemeGuid);
            string schemeAlias = ResolveKnownAlias(schemeGuid);

            List<Guid> subgroups = EnumeratePowerGuids(schemeGuid, null, ACCESS_SUBGROUP);
            if (subgroupFilter.HasValue)
            {
                if (!subgroups.Contains(subgroupFilter.Value))
                    throw new NoPowerShellException("Subgroup '{0}' does not exist in scheme '{1}'.", subgroupFilter.Value, schemeGuid);

                subgroups = new List<Guid>() { subgroupFilter.Value };
            }

            foreach (Guid subgroupGuid in subgroups)
            {
                string subgroupName = ReadSubgroupFriendlyName(schemeGuid, subgroupGuid);
                string subgroupAlias = ResolveKnownAlias(subgroupGuid);

                List<Guid> settings = EnumeratePowerGuids(schemeGuid, subgroupGuid, ACCESS_INDIVIDUAL_SETTING);
                foreach (Guid settingGuid in settings)
                {
                    string settingName = ReadSettingFriendlyName(schemeGuid, subgroupGuid, settingGuid);
                    string settingAlias = ResolveKnownAlias(settingGuid);

                    uint acValue = ReadCurrentPowerValueIndex(schemeGuid, subgroupGuid, settingGuid, true);
                    uint dcValue = ReadCurrentPowerValueIndex(schemeGuid, subgroupGuid, settingGuid, false);

                    uint minValue = 0;
                    bool hasMin = false;
                    uint maxValue = 0;
                    bool hasMax = false;
                    uint incrementValue = 0;
                    bool hasIncrement = false;
                    string units = string.Empty;
                    List<PossibleSettingValue> possibleValues = ReadPossibleSettingValues(schemeGuid, subgroupGuid, settingGuid);

                    if (possibleValues.Count > 0)
                    {
                        foreach (PossibleSettingValue possibleValue in possibleValues)
                        {
                            AddQueryResultRecord(
                                schemeGuid,
                                schemeName,
                                schemeAlias,
                                subgroupGuid,
                                subgroupName,
                                subgroupAlias,
                                settingGuid,
                                settingName,
                                settingAlias,
                                "Enumeration",
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                string.Empty,
                                FormatPossibleIndex(possibleValue.Index),
                                possibleValue.FriendlyName,
                                ToHexUInt32(acValue),
                                ToHexUInt32(dcValue)
                            );
                        }
                    }
                    else
                    {
                        string valueModel = (hasMin || hasMax || hasIncrement || !string.IsNullOrWhiteSpace(units)) ? "Range" : "Unknown";

                        AddQueryResultRecord(
                            schemeGuid,
                            schemeName,
                            schemeAlias,
                            subgroupGuid,
                            subgroupName,
                            subgroupAlias,
                            settingGuid,
                            settingName,
                            settingAlias,
                            valueModel,
                            hasMin ? ToHexUInt32(minValue) : string.Empty,
                            hasMax ? ToHexUInt32(maxValue) : string.Empty,
                            hasIncrement ? ToHexUInt32(incrementValue) : string.Empty,
                            units,
                            string.Empty,
                            string.Empty,
                            ToHexUInt32(acValue),
                            ToHexUInt32(dcValue)
                        );
                    }
                }
            }
        }

        private void AddQueryResultRecord(
            Guid schemeGuid,
            string schemeName,
            string schemeAlias,
            Guid subgroupGuid,
            string subgroupName,
            string subgroupAlias,
            Guid settingGuid,
            string settingName,
            string settingAlias,
            string settingValueModel,
            string minPossibleHex,
            string maxPossibleHex,
            string possibleIncrementHex,
            string possibleUnits,
            string possibleSettingIndex,
            string possibleSettingName,
            string currentAcValueHex,
            string currentDcValueHex)
        {
            _results.Add(new ResultRecord()
            {
                { "SchemeGuid", schemeGuid.ToString() },
                { "SchemeName", NullToEmpty(schemeName) },
                { "SchemeAlias", NullToEmpty(schemeAlias) },
                { "SubgroupGuid", subgroupGuid.ToString() },
                { "SubgroupName", NullToEmpty(subgroupName) },
                { "SubgroupAlias", NullToEmpty(subgroupAlias) },
                { "SettingGuid", settingGuid.ToString() },
                { "SettingName", NullToEmpty(settingName) },
                { "SettingAlias", NullToEmpty(settingAlias) },
                { "SettingValueModel", NullToEmpty(settingValueModel) },
                { "MinPossibleHex", NullToEmpty(minPossibleHex) },
                { "MaxPossibleHex", NullToEmpty(maxPossibleHex) },
                { "PossibleIncrementHex", NullToEmpty(possibleIncrementHex) },
                { "PossibleUnits", NullToEmpty(possibleUnits) },
                { "PossibleSettingIndex", NullToEmpty(possibleSettingIndex) },
                { "PossibleSettingName", NullToEmpty(possibleSettingName) },
                { "CurrentAcValueHex", NullToEmpty(currentAcValueHex) },
                { "CurrentDcValueHex", NullToEmpty(currentDcValueHex) }
            });
        }

        private static string NullToEmpty(string value)
        {
            return value ?? string.Empty;
        }

        private static Guid ParseGuidArgument(string value, string argumentName)
        {
            Guid guid;
            if (!Guid.TryParse(value, out guid))
                throw new NoPowerShellException("{0} must be a valid GUID.", argumentName);

            return guid;
        }

        private static List<Guid> EnumeratePowerGuids(Guid schemeGuid, Guid? subgroupGuid, uint accessType)
        {
            List<Guid> guids = new List<Guid>();
            uint index = 0;
            uint guidSize = (uint)Marshal.SizeOf(typeof(Guid));

            IntPtr schemePointer = IntPtr.Zero;
            IntPtr subgroupPointer = IntPtr.Zero;

            try
            {
                if (accessType != ACCESS_SCHEME)
                    schemePointer = AllocGuidPointer(schemeGuid);

                if (subgroupGuid.HasValue)
                    subgroupPointer = AllocGuidPointer(subgroupGuid.Value);

                while (true)
                {
                    uint localBufferSize = guidSize;
                    IntPtr outputGuidPointer = Marshal.AllocHGlobal((int)localBufferSize);

                    try
                    {
                        uint status = NativeMethods.PowerEnumerate(
                            IntPtr.Zero,
                            schemePointer,
                            subgroupPointer,
                            accessType,
                            index,
                            outputGuidPointer,
                            ref localBufferSize
                        );

                        if (status == ERROR_NO_MORE_ITEMS)
                            break;

                        EnsureSuccess(status, "PowerEnumerate");

                        Guid enumeratedGuid = (Guid)Marshal.PtrToStructure(outputGuidPointer, typeof(Guid));
                        guids.Add(enumeratedGuid);
                        index++;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(outputGuidPointer);
                    }
                }
            }
            finally
            {
                if (schemePointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(schemePointer);

                if (subgroupPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(subgroupPointer);
            }

            return guids;
        }

        private static IntPtr AllocGuidPointer(Guid guid)
        {
            IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
            Marshal.StructureToPtr(guid, pointer, false);
            return pointer;
        }

        private static string ReadSubgroupFriendlyName(Guid schemeGuid, Guid subgroupGuid)
        {
            IntPtr subgroupPointer = IntPtr.Zero;
            try
            {
                subgroupPointer = AllocGuidPointer(subgroupGuid);
                return ReadFriendlyNameCore(schemeGuid, subgroupPointer, IntPtr.Zero);
            }
            finally
            {
                if (subgroupPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(subgroupPointer);
            }
        }

        private static string ReadSettingFriendlyName(Guid schemeGuid, Guid subgroupGuid, Guid settingGuid)
        {
            IntPtr subgroupPointer = IntPtr.Zero;
            IntPtr settingPointer = IntPtr.Zero;
            try
            {
                subgroupPointer = AllocGuidPointer(subgroupGuid);
                settingPointer = AllocGuidPointer(settingGuid);
                return ReadFriendlyNameCore(schemeGuid, subgroupPointer, settingPointer);
            }
            finally
            {
                if (settingPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(settingPointer);

                if (subgroupPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(subgroupPointer);
            }
        }

        private static string ReadFriendlyNameCore(Guid schemeGuid, IntPtr subgroupPointer, IntPtr settingPointer)
        {
            uint bufferSize = 0;
            uint status = NativeMethods.PowerReadFriendlyName(
                IntPtr.Zero,
                ref schemeGuid,
                subgroupPointer,
                settingPointer,
                IntPtr.Zero,
                ref bufferSize
            );

            if (status == ERROR_FILE_NOT_FOUND)
                return string.Empty;

            if (status != 0 && status != ERROR_MORE_DATA)
                EnsureSuccess(status, "PowerReadFriendlyName");

            if (bufferSize == 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = NativeMethods.PowerReadFriendlyName(
                    IntPtr.Zero,
                    ref schemeGuid,
                    subgroupPointer,
                    settingPointer,
                    buffer,
                    ref bufferSize
                );

                if (status == ERROR_FILE_NOT_FOUND)
                    return string.Empty;

                EnsureSuccess(status, "PowerReadFriendlyName");

                byte[] rawName = new byte[bufferSize];
                Marshal.Copy(buffer, rawName, 0, (int)bufferSize);
                return Encoding.Unicode.GetString(rawName).TrimEnd('\0');
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static uint ReadCurrentPowerValueIndex(Guid schemeGuid, Guid subgroupGuid, Guid settingGuid, bool ac)
        {
            uint value;
            uint status;

            if (ac)
                status = NativeMethods.PowerReadACValueIndex(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, out value);
            else
                status = NativeMethods.PowerReadDCValueIndex(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, out value);

            EnsureSuccess(status, ac ? "PowerReadACValueIndex" : "PowerReadDCValueIndex");
            return value;
        }

        private delegate uint ReadPowerUInt32MetadataDelegate(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupGuid, ref Guid settingGuid, out uint value);

        private static bool TryReadUInt32Metadata(ReadPowerUInt32MetadataDelegate readDelegate, Guid schemeGuid, Guid subgroupGuid, Guid settingGuid, out uint value)
        {
            value = 0;

            uint status = readDelegate(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, out value);
            if (status == ERROR_FILE_NOT_FOUND)
                return false;

            EnsureSuccess(status, "PowerReadMetadata");
            return true;
        }

        private static string ReadSettingUnits(Guid schemeGuid, Guid subgroupGuid, Guid settingGuid)
        {
            uint bufferSize = 0;
            uint status = NativeMethods.PowerReadValueUnitsSpecifier(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, IntPtr.Zero, ref bufferSize);

            if (status == ERROR_FILE_NOT_FOUND)
                return string.Empty;

            if (status != 0 && status != ERROR_MORE_DATA)
                EnsureSuccess(status, "PowerReadValueUnitsSpecifier");

            if (bufferSize == 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = NativeMethods.PowerReadValueUnitsSpecifier(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, buffer, ref bufferSize);
                EnsureSuccess(status, "PowerReadValueUnitsSpecifier");

                byte[] rawUnits = new byte[bufferSize];
                Marshal.Copy(buffer, rawUnits, 0, (int)bufferSize);
                return Encoding.Unicode.GetString(rawUnits).TrimEnd('\0');
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static List<PossibleSettingValue> ReadPossibleSettingValues(Guid schemeGuid, Guid subgroupGuid, Guid settingGuid)
        {
            List<PossibleSettingValue> possibleValues = new List<PossibleSettingValue>();

            uint index = 0;
            while (true)
            {
                uint indexValue;
                uint status = ReadPossibleValueIndex(schemeGuid, subgroupGuid, settingGuid, index, out indexValue);

                if (status == ERROR_NO_MORE_ITEMS || status == ERROR_FILE_NOT_FOUND)
                    break;

                EnsureSuccess(status, "PowerReadPossibleValue");

                string friendlyName = ReadPossibleFriendlyName(schemeGuid, subgroupGuid, settingGuid, index);
                possibleValues.Add(new PossibleSettingValue(indexValue, friendlyName));
                index++;
            }

            return possibleValues;
        }

        private static uint ReadPossibleValueIndex(Guid schemeGuid, Guid subgroupGuid, Guid settingGuid, uint index, out uint indexValue)
        {
            indexValue = 0;
            uint bufferSize = sizeof(uint);
            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);

            try
            {
                uint status = NativeMethods.PowerReadPossibleValue(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, index, buffer, ref bufferSize);
                if (status != 0)
                    return status;

                if (bufferSize < sizeof(uint))
                    throw new NoPowerShellException("PowerReadPossibleValue returned an unexpected value size.");

                indexValue = (uint)Marshal.ReadInt32(buffer);
                return status;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string ReadPossibleFriendlyName(Guid schemeGuid, Guid subgroupGuid, Guid settingGuid, uint index)
        {
            uint bufferSize = 0;
            uint status = NativeMethods.PowerReadPossibleFriendlyName(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, index, IntPtr.Zero, ref bufferSize);

            if (status == ERROR_FILE_NOT_FOUND)
                return string.Empty;

            if (status != 0 && status != ERROR_MORE_DATA)
                EnsureSuccess(status, "PowerReadPossibleFriendlyName");

            if (bufferSize == 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
            try
            {
                status = NativeMethods.PowerReadPossibleFriendlyName(IntPtr.Zero, ref schemeGuid, ref subgroupGuid, ref settingGuid, index, buffer, ref bufferSize);
                EnsureSuccess(status, "PowerReadPossibleFriendlyName");

                byte[] rawName = new byte[bufferSize];
                Marshal.Copy(buffer, rawName, 0, (int)bufferSize);
                return Encoding.Unicode.GetString(rawName).TrimEnd('\0');
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string FormatPossibleIndex(uint indexValue)
        {
            return indexValue.ToString("000");
        }

        private static string ToHexUInt32(uint value)
        {
            return string.Format("0x{0:x8}", value);
        }

        private static string ResolveKnownAlias(Guid guid)
        {
            string alias;
            if (KNOWN_GUID_ALIASES.TryGetValue(guid, out alias))
                return alias;

            return string.Empty;
        }

        private struct PossibleSettingValue
        {
            public PossibleSettingValue(uint index, string friendlyName)
            {
                Index = index;
                FriendlyName = friendlyName;
            }

            public uint Index { get; private set; }
            public string FriendlyName { get; private set; }
        }

        private static void ValidateNonNegativeTimeout(IntegerArgument argument, string argumentName)
        {
            if (argument.IsSet && argument.Value < 0)
                throw new NoPowerShellException("{0} must be greater than or equal to 0.", argumentName);
        }

        private void SetActiveScheme(string stepName, Guid schemeGuid)
        {
            uint status = NativeMethods.PowerSetActiveScheme(IntPtr.Zero, ref schemeGuid);
            EnsureSuccess(status, stepName);

            _results.Add(new ResultRecord()
            {
                { "Step", stepName },
                { "Action", "PowerSetActiveScheme" },
                { "SchemeGuid", schemeGuid.ToString() }
            });
        }

        private void WritePowerValue(string stepName, bool acValue, Guid subGroupGuid, Guid settingGuid, uint value)
        {
            Guid activeSchemeGuid = GetActiveSchemeGuid();
            uint status;

            if (acValue)
            {
                status = NativeMethods.PowerWriteACValueIndex(IntPtr.Zero, ref activeSchemeGuid, ref subGroupGuid, ref settingGuid, value);
            }
            else
            {
                status = NativeMethods.PowerWriteDCValueIndex(IntPtr.Zero, ref activeSchemeGuid, ref subGroupGuid, ref settingGuid, value);
            }

            EnsureSuccess(status, stepName);

            _results.Add(new ResultRecord()
            {
                { "Step", stepName },
                { "Action", acValue ? "PowerWriteACValueIndex" : "PowerWriteDCValueIndex" },
                { "SchemeGuid", activeSchemeGuid.ToString() },
                { "SubGroupGuid", subGroupGuid.ToString() },
                { "SettingGuid", settingGuid.ToString() },
                { "Value", value.ToString() }
            });
        }

        private static Guid ResolveSchemeIdentifier(string schemeIdentifier)
        {
            if (schemeIdentifier.Equals("scheme_current", StringComparison.InvariantCultureIgnoreCase))
                return GetActiveSchemeGuid();

            Guid schemeGuid;
            if (!Guid.TryParse(schemeIdentifier, out schemeGuid))
            {
                throw new NoPowerShellException("SetActive expects a power scheme GUID or the value 'scheme_current'.");
            }

            return schemeGuid;
        }

        private static Guid GetActiveSchemeGuid()
        {
            IntPtr activeSchemeGuidPointer = IntPtr.Zero;
            try
            {
                uint status = NativeMethods.PowerGetActiveScheme(IntPtr.Zero, out activeSchemeGuidPointer);
                EnsureSuccess(status, "GetActiveScheme");

                if (activeSchemeGuidPointer == IntPtr.Zero)
                    throw new NoPowerShellException("PowerGetActiveScheme returned an empty scheme pointer.");

                return (Guid)Marshal.PtrToStructure(activeSchemeGuidPointer, typeof(Guid));
            }
            finally
            {
                if (activeSchemeGuidPointer != IntPtr.Zero)
                    NativeMethods.LocalFree(activeSchemeGuidPointer);
            }
        }

        private static void EnsureSuccess(uint status, string operation)
        {
            if (status == 0)
                return;

            Win32Exception win32Exception = new Win32Exception((int)status);
            throw new NoPowerShellException("{0} failed with Win32 error 0x{1:X8}: {2}", operation, status, win32Exception.Message);
        }

        private static readonly Guid GUID_SLEEP_SUBGROUP = new Guid("238c9fa8-0aad-41ed-83f4-97be242c8f20");
        private static readonly Guid GUID_DISK_SUBGROUP = new Guid("0012ee47-9041-4b5d-9b77-535fba8b1442");
        private static readonly Guid GUID_HIBERNATE_TIMEOUT = new Guid("9d7815a6-7ee4-497e-8888-515a05f02364");
        private static readonly Guid GUID_STANDBY_TIMEOUT = new Guid("29f6c1db-86da-48c5-9fdb-f2b67b1f44da");
        private static readonly Guid GUID_HYBRID_SLEEP = new Guid("94ac6d29-73ce-41a6-809f-6363ba21b47e");
        private static readonly Guid GUID_ALLOW_WAKE_TIMERS = new Guid("bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d");
        private static readonly Guid GUID_DISK_IDLE = new Guid("6738e2c4-e8a5-4a42-b16a-e040e769756e");
        private static readonly Guid GUID_BUTTONS_SUBGROUP = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");
        private static readonly Guid GUID_LID_ACTION = new Guid("5ca83367-6e45-459f-a27b-476b1d01c936");
        private const uint ACCESS_SCHEME = 16;
        private const uint ACCESS_SUBGROUP = 17;
        private const uint ACCESS_INDIVIDUAL_SETTING = 18;
        private const uint ERROR_NO_MORE_ITEMS = 259;
        private const uint ERROR_MORE_DATA = 234;
        private const uint ERROR_FILE_NOT_FOUND = 2;

        private static readonly Dictionary<Guid, string> KNOWN_GUID_ALIASES = new Dictionary<Guid, string>()
        {
            { new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"), "SCHEME_MIN" },
            { GUID_SLEEP_SUBGROUP, "SUB_SLEEP" },
            { GUID_DISK_SUBGROUP, "SUB_DISK" },
            { GUID_BUTTONS_SUBGROUP, "SUB_BUTTONS" },
            { GUID_STANDBY_TIMEOUT, "STANDBYIDLE" },
            { GUID_HYBRID_SLEEP, "HYBRIDSLEEP" },
            { GUID_HIBERNATE_TIMEOUT, "HIBERNATEIDLE" },
            { GUID_ALLOW_WAKE_TIMERS, "RTCWAKE" },
            { GUID_DISK_IDLE, "DISKIDLE" }
        };

        private static class NativeMethods
        {
            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerGetActiveScheme(IntPtr rootPowerKey, out IntPtr activePolicyGuid);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerSetActiveScheme(IntPtr rootPowerKey, ref Guid schemeGuid);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerWriteACValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupGuid, ref Guid powerSettingGuid, uint acValueIndex);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerWriteDCValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupGuid, ref Guid powerSettingGuid, uint dcValueIndex);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerEnumerate(IntPtr rootPowerKey, IntPtr schemeGuid, IntPtr subGroupOfPowerSettingsGuid, uint accessFlags, uint index, IntPtr buffer, ref uint bufferSize);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subGroupOfPowerSettingsGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadACValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint acValueIndex);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadDCValueIndex(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint dcValueIndex);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadValueMin(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint valueMinimum);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadValueMax(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint valueMaximum);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadValueIncrement(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, out uint valueIncrement);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadValueUnitsSpecifier(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, IntPtr buffer, ref uint bufferSize);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadPossibleValue(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, uint possibleSettingIndex, IntPtr buffer, ref uint bufferSize);

            [DllImport("powrprof.dll", SetLastError = true)]
            public static extern uint PowerReadPossibleFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, ref Guid subGroupOfPowerSettingsGuid, ref Guid powerSettingGuid, uint possibleSettingIndex, IntPtr buffer, ref uint bufferSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LocalFree(IntPtr hMem);
        }
    }
}
