using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetItemPropertyValueCommand : PSCommand
    {
        public GetItemPropertyValueCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string name = _arguments.Get<StringArgument>("Name").Value;
            CaseInsensitiveList attributeNames = null;
            if (!string.IsNullOrEmpty(name))
                attributeNames = new CaseInsensitiveList(name.Split(','));

            // Registry:
            //     HKLM:\
            //     HKCU:\
            //     HKCR:\
            //     HKU:\
            if (RegistryHelper.IsRegistryPath(path))
                _results = BrowseRegistry(path, attributeNames);

            // Filesystem:
            //     \
            //     ..\
            //     D:\
            else // TODO
                throw new NoPowerShellException("At this moment Get-ItemPropertyValue only supports registry.");

            return _results;
        }

        private CommandResult BrowseRegistry(string path, CaseInsensitiveList attributeNames)
        {
            RegistryHive root = RegistryHelper.GetRoot(ref path);

            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64))
            {
                using (RegistryKey key = baseKey.OpenSubKey(path))
                {
                    foreach (string attr in attributeNames)
                    {
                        object value = key.GetValue(attr);
                        RegistryValueKind kind = key.GetValueKind(attr);

                        string strValue;
                        switch (kind)
                        {
                            case RegistryValueKind.DWord:
                                strValue = Convert.ToInt32(value).ToString();
                                break;
                            case RegistryValueKind.QWord:
                                strValue = Convert.ToInt64(value).ToString();
                                break;
                            case RegistryValueKind.MultiString:
                                strValue = string.Join(";", (string[])value);
                                break;
                            case RegistryValueKind.String:
                            case RegistryValueKind.ExpandString:
                                strValue = value.ToString();
                                break;
                            case RegistryValueKind.Binary:
                                byte[] bValue = (byte[])value;
                                _results.Add(
                                    new ResultRecord()
                                    {
                                { "Value", System.Text.Encoding.ASCII.GetString(bValue) },
                                { "Hex", BitConverter.ToString(bValue).Replace('-', ' ') },
                                { "Base64", Convert.ToBase64String(bValue) }
                                    }
                                );
                                continue;
                            default:
                                strValue = value.ToString();
                                break;
                        }

                        _results.Add(
                            new ResultRecord()
                            {
                        { "Value", strValue }
                            }
                        );
                    }
                }
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ItemPropertyValue", "gpv" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path", false),
                    new StringArgument("Name", false)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the value for one or more properties of a specified item."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Show current user's PATH variable",
                        new List<string>()
                        {
                            @"Get-ItemPropertyValue -Path HKCU:\Environment -Name Path",
                            @"gpv HKCU:\Environment Path"
                        }
                    )
                };
            }
        }
    }
}
