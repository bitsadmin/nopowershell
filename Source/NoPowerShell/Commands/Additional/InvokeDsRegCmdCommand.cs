using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Additional
{
    public class InvokeDsRegCmdCommand : PSCommand
    {
        public InvokeDsRegCmdCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string[] options = BuildOptions();

            Console.OutputEncoding = Encoding.Unicode;
            DsregModule = LoadDsregModule();
            EnsureNativeLibraryLoaded();
            InvokeDsrCliFromMtaThread(options);

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Invoke-DsRegCmd",
            "dsregcmd"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new BoolArgument("status"),
            new BoolArgument("status_old"),
            new BoolArgument("join"),
            new BoolArgument("leave"),
            new BoolArgument("debug"),
            new BoolArgument("refreshprt"),
            new BoolArgument("refreshp2pcerts"),
            new BoolArgument("cleanupaccounts"),
            new BoolArgument("listaccounts"),
            new BoolArgument("UpdateDevice")
        };

        public static new string Synopsis => "Execute dsreg.dll DsrCLI options for device registration and account maintenance.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Displays the device join status", "Invoke-DsRegCmd -Status"),
            new ExampleEntry("Displays the device join status in old format", "Invoke-DsRegCmd -Status_Old"),
            new ExampleEntry("Schedules and monitors the Autojoin task to Hybrid Join the device", "Invoke-DsRegCmd -Join"),
            new ExampleEntry("Performs Hybrid Unjoin", "Invoke-DsRegCmd -Leave"),
            new ExampleEntry("Displays debug messages", "Invoke-DsRegCmd -Debug"),
            new ExampleEntry("Refreshes PRT in the CloudAP cache", "Invoke-DsRegCmd -RefreshPRT"),
            new ExampleEntry("Refreshes P2P certificates", "Invoke-DsRegCmd -RefreshP2PCerts"),
            new ExampleEntry("Deletes all WAM accounts", "Invoke-DsRegCmd -CleanupAccounts"),
            new ExampleEntry("Lists all WAM accounts", "Invoke-DsRegCmd -ListAccounts"),
            new ExampleEntry("Update device attributes to Azure AD", "Invoke-DsRegCmd -UpdateDevice")
        };

        private string[] BuildOptions()
        {
            string[] optionNames = new string[]
            {
                "Status",
                "Status_Old",
                "Join",
                "Leave",
                "Debug",
                "RefreshPRT",
                "RefreshP2PCerts",
                "CleanupAccounts",
                "ListAccounts",
                "UpdateDevice"
            };

            List<string> options = new List<string>();
            for (int i = 0; i < optionNames.Length; i++)
            {
                if (_arguments.Get<BoolArgument>(optionNames[i]).Value)
                {
                    options.Add("/" + optionNames[i]);
                }
            }

            if (!options.Any())
            {
                options.Add("/?");
            }

            return options.ToArray();
        }

        private static void EnsureNativeLibraryLoaded()
        {
            IntPtr module = DsregModule;
            if (module == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new DllNotFoundException(
                    "Could not load dsreg.dll from System32/SysNative. Win32Error=0x" + errorCode.ToString("X8")
                );
            }
        }

        private static int InvokeDsrCli(string[] options)
        {
            IntPtr argvBuffer = IntPtr.Zero;
            IntPtr[] argvPointers = null;

            try
            {
                argvPointers = new IntPtr[options.Length];
                argvBuffer = Marshal.AllocHGlobal(IntPtr.Size * options.Length);

                for (int i = 0; i < options.Length; i++)
                {
                    argvPointers[i] = Marshal.StringToHGlobalUni(options[i]);
                    Marshal.WriteIntPtr(argvBuffer, i * IntPtr.Size, argvPointers[i]);
                }

                return NativeMethods.DsrCLI(options.Length, argvBuffer, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                if (argvPointers != null)
                {
                    for (int i = 0; i < argvPointers.Length; i++)
                    {
                        if (argvPointers[i] != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(argvPointers[i]);
                        }
                    }
                }

                if (argvBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(argvBuffer);
                }
            }
        }

        private static void InvokeDsrCliFromMtaThread(string[] options)
        {
            Exception invocationException = null;

            Thread thread = new Thread(() =>
            {
                try
                {
                    InvokeDsrCli(options);
                }
                catch (Exception ex)
                {
                    invocationException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            thread.Join();

            if (invocationException != null)
            {
                throw invocationException;
            }
        }

        private static IntPtr LoadDsregModule()
        {
            IntPtr module = NativeMethods.GetModuleHandle("dsreg.dll");
            if (module != IntPtr.Zero)
            {
                return module;
            }

            string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string system32Path = System.IO.Path.Combine(systemDirectory, "dsreg.dll");
            module = NativeMethods.LoadLibraryEx(system32Path, IntPtr.Zero, 0);
            if (module != IntPtr.Zero)
            {
                return module;
            }

            if (!Environment.Is64BitProcess)
            {
                string windowsDirectory = Environment.GetEnvironmentVariable("WINDIR") ?? "C:\\Windows";
                string sysnativePath = System.IO.Path.Combine(windowsDirectory, "SysNative", "dsreg.dll");
                module = NativeMethods.LoadLibraryEx(sysnativePath, IntPtr.Zero, 0);
                if (module != IntPtr.Zero)
                {
                    return module;
                }
            }

            return NativeMethods.LoadLibraryEx("dsreg.dll", IntPtr.Zero, 0);
        }

        private static IntPtr DsregModule;

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(string lpModuleName);

            [DllImport("dsreg.dll", EntryPoint = "DsrCLI", CharSet = CharSet.Unicode)]
            internal static extern int DsrCLI(int argc, IntPtr argv, IntPtr cliExtensions, IntPtr contextGuid);
        }
    }
}
