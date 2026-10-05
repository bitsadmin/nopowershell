using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
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
            new StringArgument("SetActive", true),
            new IntegerArgument("SetHibernateTimeoutAc", 0),
            new IntegerArgument("SetHibernateTimeoutDc", 0),
            new IntegerArgument("SetStandbyTimeoutAc", 0),
            new IntegerArgument("SetStandbyTimeoutDc", 0),
            new BoolArgument("DisableLidSleep"),
            new BoolArgument("EnableLidSleep"),
            new BoolArgument("ReactivateCurrentScheme")
        };

        public static new string Synopsis => "Manage power scheme settings using native Windows power APIs.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("List all available power schemes", "Invoke-PowerCfg -List"),
            new ExampleEntry("Set active scheme to high performance", "Invoke-PowerCfg -SetActive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
            new ExampleEntry("Disable hibernate timeout", "Invoke-PowerCfg -SetHibernateTimeoutAc 0 -SetHibernateTimeoutDc 0"),
            new ExampleEntry("Disable standby timeout", "Invoke-PowerCfg -SetStandbyTimeoutAc 0 -SetStandbyTimeoutDc 0"),
            new ExampleEntry("Disable sleep when closing lid", "Invoke-PowerCfg -DisableLidSleep"),
            new ExampleEntry("Enable sleep when closing lid", "Invoke-PowerCfg -EnableLidSleep"),
            new ExampleEntry("Re-activate current scheme", "Invoke-PowerCfg -ReactivateCurrentScheme")
        };

        private static void ValidateArguments(
            bool list,
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

            ValidateNonNegativeTimeout(setHibernateTimeoutAcArgument, "SetHibernateTimeoutAc");
            ValidateNonNegativeTimeout(setHibernateTimeoutDcArgument, "SetHibernateTimeoutDc");
            ValidateNonNegativeTimeout(setStandbyTimeoutAcArgument, "SetStandbyTimeoutAc");
            ValidateNonNegativeTimeout(setStandbyTimeoutDcArgument, "SetStandbyTimeoutDc");

            bool hasAction =
                list ||
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
        private static readonly Guid GUID_HIBERNATE_TIMEOUT = new Guid("9d7815a6-7ee4-497e-8888-515a05f02364");
        private static readonly Guid GUID_STANDBY_TIMEOUT = new Guid("29f6c1db-86da-48c5-9fdb-f2b67b1f44da");
        private static readonly Guid GUID_BUTTONS_SUBGROUP = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");
        private static readonly Guid GUID_LID_ACTION = new Guid("5ca83367-6e45-459f-a27b-476b1d01c936");
        private const uint ACCESS_SCHEME = 16;
        private const uint ERROR_NO_MORE_ITEMS = 259;
        private const uint ERROR_MORE_DATA = 234;

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

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LocalFree(IntPtr hMem);
        }
    }
}
