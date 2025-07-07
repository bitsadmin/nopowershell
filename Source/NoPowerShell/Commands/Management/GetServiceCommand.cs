using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Security.Principal;
using System.ComponentModel;
using System.Linq;
using System.Security;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetServiceCommand : PSCommand
    {
        public GetServiceCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect optional ComputerName, Username, and Password parameters
            base.Execute();

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

                // Process each service
                foreach (ServiceController service in filteredServices)
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

        private bool IsLocalhost(string computername)
        {
            return string.IsNullOrWhiteSpace(computername) ||
                computername.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                computername.Equals(".");
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

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Service", "gsv" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList
                {
                    new StringArgument("Name", true),
                    new StringArgument("DisplayName", true),
                    new StringArgument("Include", true),
                    new StringArgument("Exclude", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the services on a local or remote computer."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries
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
                    )
                };
            }
        }
    }
}