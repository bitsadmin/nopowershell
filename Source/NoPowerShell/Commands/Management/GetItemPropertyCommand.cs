using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using Microsoft.Win32;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetItemPropertyCommand : PSCommand
    {
        public GetItemPropertyCommand(string[] arguments) : base(arguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain parameters
            bool includeHidden = _arguments.Get<BoolArgument>("Force").Value;
            string path = _arguments.Get<StringArgument>("Path").Value;
            string[] searchPatterns = _arguments.Get<StringArgument>("Include").Value.Split(new char[] { ',' });
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
            else
                _results = GetChildItemCommand.BrowseFilesystem(path, false, 1, includeHidden, searchPatterns);

            return _results;
        }

        private CommandResult BrowseRegistry(string path, CaseInsensitiveList attributeNames)
        {
            RegistryKey root = RegistryHelper.GetRoot(ref path);

            RegistryKey key = root.OpenSubKey(path);
            foreach (string valueName in key.GetValueNames())
            {
                string valueKind = key.GetValueKind(valueName).ToString();
                string value = Convert.ToString(key.GetValue(valueName));

                // Skip if -Names parameter is provided and current attribute is not in the list
                if (attributeNames != null && !attributeNames.Contains(valueName))
                    continue;

                _results.Add(
                    new ResultRecord()
                    {
                        { "Name", valueName },
                        { "Kind", valueKind },
                        { "Value", value }
                    }
                );
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ItemProperty", "gp" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path", "."),
                    new BoolArgument("Force") ,
                    new StringArgument("Include", "*", true),
                    new StringArgument("Name", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the properties of a specified item."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List autoruns in the registry", @"Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Run | ft")
                };
            }
        }
    }
}
