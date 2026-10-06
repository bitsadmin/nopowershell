using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetServiceCommand : PSCommand
    {
        private const uint SC_MANAGER_CONNECT = 0x0001;
        private const uint READ_CONTROL = 0x00020000;
        private const uint OWNER_SECURITY_INFORMATION = 0x00000001;
        private const uint GROUP_SECURITY_INFORMATION = 0x00000002;
        private const uint DACL_SECURITY_INFORMATION = 0x00000004;
        private const uint SERVICE_SECURITY_INFORMATION = OWNER_SECURITY_INFORMATION | GROUP_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION;
        private const uint SDDL_REVISION_1 = 1;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool QueryServiceObjectSecurity(IntPtr hService, uint dwSecurityInformation, byte[] lpSecurityDescriptor, uint cbBufSize, out uint pcbBytesNeeded);

        [DllImport("advapi32.dll", EntryPoint = "ConvertSecurityDescriptorToStringSecurityDescriptorW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(byte[] securityDescriptor, uint requestedStringSDRevision, uint securityInformation, out IntPtr stringSecurityDescriptor, out uint stringSecurityDescriptorLen);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        public GetServiceCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            // Collect optional ComputerName, Username, and Password parameters
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            // Obtain cmdlet parameters
            string name = _arguments.Get<StringArgument>("Name").Value;
            string displayName = _arguments.Get<StringArgument>("DisplayName").Value;
            string includeString = _arguments.Get<StringArgument>("Include").Value;
            string[] include = new string[0];
            if (!string.IsNullOrEmpty(includeString))
                include = includeString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string excludeString = _arguments.Get<StringArgument>("Exclude").Value;
            string[] exclude = new string[0];
            if (!string.IsNullOrEmpty(excludeString))
                exclude = excludeString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (!string.IsNullOrWhiteSpace(name) && name.Equals("scmanager", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    throw new NoPowerShellException("When using -Name scmanager, do not specify -DisplayName.");
                }

                ResultRecord scmRecord = new ResultRecord
                {
                    { "Name", "scmanager" },
                    { "DisplayName", "Service Control Manager" },
                    { "Status", "N/A" },
                    { "ServiceType", "N/A" },
                    { "StartType", "N/A" },
                    { "CanPauseAndContinue", "N/A" },
                    { "CanStop", "N/A" },
                    { "DependentServices", string.Empty },
                    { "ServicesDependedOn", string.Empty },
                    { "Sddl", GetScManagerSecurityDescriptorSddl(computername) }
                };

                _results.Add(scmRecord);
                return _results;
            }

            try
            {
                // Use ServiceController to get services
                ServiceController[] services;
                if (IsLocalhost(computername))
                {
                    services = ServiceController.GetServices();
                }
                else
                {
                    services = ServiceController.GetServices(computername);
                }

                // Filter services based on parameters
                IEnumerable<ServiceController> filteredServices = services;

                // Filter by Name (ServiceName)
                if (!string.IsNullOrEmpty(name))
                {
                    filteredServices = filteredServices.Where(s => s.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by DisplayName
                if (!string.IsNullOrEmpty(displayName))
                {
                    filteredServices = filteredServices.Where(s => s.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
                }

                // Apply Include filter
                if (include.Length > 0)
                {
                    filteredServices = filteredServices.Where(s =>
                        include.Any(i => s.ServiceName.IndexOf(i.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       s.DisplayName.IndexOf(i.Trim(), StringComparison.OrdinalIgnoreCase) >= 0));
                }

                // Apply Exclude filter
                if (exclude.Length > 0)
                {
                    filteredServices = filteredServices.Where(s =>
                        !exclude.Any(e => s.ServiceName.IndexOf(e.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 ||
                                        s.DisplayName.IndexOf(e.Trim(), StringComparison.OrdinalIgnoreCase) >= 0));
                }

                List<ServiceController> filteredServiceList = filteredServices.ToList();

                // Process each service
                foreach (ServiceController service in filteredServiceList)
                {
                    try
                    {
                        ResultRecord record = new ResultRecord
                        {
                            { "Name", service.ServiceName },
                            { "DisplayName", service.DisplayName },
                            { "Status", service.Status.ToString() },
                            { "ServiceType", service.ServiceType.ToString() },
                            { "StartType", GetStartType(service) },
                            { "CanPauseAndContinue", service.CanPauseAndContinue.ToString() },
                            { "CanStop", service.CanStop.ToString() },
                            { "DependentServices", string.Join(", ", service.DependentServices.Select(ds => ds.ServiceName)) },
                            { "ServicesDependedOn", string.Join(", ", service.ServicesDependedOn.Select(ds => ds.ServiceName)) }
                        };

                        record.Add("Sddl", GetServiceSecurityDescriptorSddl(service.ServiceName, computername));

                        // Add ComputerName if specified
                        if (!IsLocalhost(computername))
                        {
                            record.Add("ComputerName", computername);
                        }

                        _results.Add(record);
                    }
                    catch (Exception ex)
                    {
                        _results.Add(new ResultRecord
                        {
                            { string.Empty, $"Error processing service {service.ServiceName}: {ex.Message}" }
                        });
                    }
                }
            }
            catch (Win32Exception ex)
            {
                _results.Add(new ResultRecord
                {
                    { string.Empty, $"Error accessing services: {ex.Message}" }
                });
            }
            catch (SecurityException ex)
            {
                _results.Add(new ResultRecord
                {
                    { string.Empty, $"Security error: {ex.Message}" }
                });
            }

            return _results;
        }

        private static bool IsLocalhost(string computername)
        {
            return string.IsNullOrWhiteSpace(computername) ||
                computername.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                computername.Equals(".");
        }

        private static string GetScManagerSecurityDescriptorSddl(string computername)
        {
            try
            {
                string machineName = IsLocalhost(computername) ? null : computername;
                IntPtr scmHandle = OpenSCManager(machineName, null, SC_MANAGER_CONNECT | READ_CONTROL);
                if (scmHandle == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager.");

                try
                {
                    return QueryObjectSecurityDescriptorSddl(scmHandle, "Service Control Manager");
                }
                finally
                {
                    CloseServiceHandle(scmHandle);
                }
            }
            catch (Win32Exception ex)
            {
                throw new NoPowerShellException("Failed to query SDDL for Service Control Manager: {0}", ex.Message);
            }
        }

        private static string GetServiceSecurityDescriptorSddl(string serviceName, string computername)
        {
            try
            {
                string machineName = IsLocalhost(computername) ? null : computername;
                IntPtr scmHandle = OpenSCManager(machineName, null, SC_MANAGER_CONNECT);
                if (scmHandle == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Service Control Manager.");

                try
                {
                    IntPtr serviceHandle = OpenService(scmHandle, serviceName, READ_CONTROL);
                    if (serviceHandle == IntPtr.Zero)
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open service '{serviceName}'.");

                    try
                    {
                        return QueryObjectSecurityDescriptorSddl(serviceHandle, serviceName);
                    }
                    finally
                    {
                        CloseServiceHandle(serviceHandle);
                    }
                }
                finally
                {
                    CloseServiceHandle(scmHandle);
                }
            }
            catch (Win32Exception ex)
            {
                throw new NoPowerShellException("Failed to query SDDL for service '{0}': {1}", serviceName, ex.Message);
            }
        }

        private static string QueryObjectSecurityDescriptorSddl(IntPtr handle, string objectName)
        {
            uint bytesNeeded;
            bool queryResult = QueryServiceObjectSecurity(handle, SERVICE_SECURITY_INFORMATION, null, 0, out bytesNeeded);
            if (!queryResult)
            {
                int firstError = Marshal.GetLastWin32Error();
                if (firstError != ERROR_INSUFFICIENT_BUFFER)
                    throw new Win32Exception(firstError, $"Failed to determine security descriptor size for '{objectName}'.");
            }

            if (bytesNeeded == 0)
                throw new NoPowerShellException($"Could not read security descriptor for '{objectName}'.");

            byte[] securityDescriptor = new byte[bytesNeeded];
            if (!QueryServiceObjectSecurity(handle, SERVICE_SECURITY_INFORMATION, securityDescriptor, bytesNeeded, out bytesNeeded))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to query security descriptor for '{objectName}'.");

            IntPtr sddlPtr;
            uint sddlLength;
            if (!ConvertSecurityDescriptorToStringSecurityDescriptor(securityDescriptor, SDDL_REVISION_1, SERVICE_SECURITY_INFORMATION, out sddlPtr, out sddlLength))
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to convert security descriptor to SDDL for '{objectName}'.");

            try
            {
                return Marshal.PtrToStringUni(sddlPtr);
            }
            finally
            {
                if (sddlPtr != IntPtr.Zero)
                    LocalFree(sddlPtr);
            }
        }

        // Helper method to get service start type
        private string GetStartType(ServiceController service)
        {
            try
            {
                // Using ServiceController doesn't directly provide StartType in .NET Framework
                // We'll use registry to get this information
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Services\{service.ServiceName}"))
                {
                    if (key != null)
                    {
                        int startValue = (int)key.GetValue("Start", -1);
                        switch (startValue)
                        {
                            case 2: return "Automatic";
                            case 3: return "Manual";
                            case 4: return "Disabled";
                            case 0: return "Boot";
                            case 1: return "System";
                            default: return "Unknown";
                        }
                    }
                }
            }
            catch
            {
                // Fallback if registry access fails
            }
            return "Unknown";
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-Service",
            "gsv"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("ComputerName", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            new StringArgument("Name", true),
            new StringArgument("DisplayName", true),
            new StringArgument("Include", true),
            new StringArgument("Exclude", true)
        };

        public static new string Synopsis => "Gets the services on a local or remote computer.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Get all services on the local computer", "Get-Service"),
            new ExampleEntry("Get a specific service by name", "Get-Service -Name wuauserv"),
            new ExampleEntry("Get services by display name", "Get-Service -DisplayName \"Windows Update\""),
            new ExampleEntry("Get services on a remote computer", "Get-Service -ComputerName MyServer"),
            new ExampleEntry(
                "Filter services using Include and Exclude",
                new List<string>
                {
                    "Get-Service -Include \"Win\"",
                    "Get-Service -Exclude \"WinRM\""
                }
            ),
            new ExampleEntry("Show security descriptor of Service Control Manager", "Get-Service -Name scmanager")
        };
    }
}