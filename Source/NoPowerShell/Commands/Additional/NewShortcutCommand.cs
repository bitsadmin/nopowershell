using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class NewShortcut : PSCommand
    {
        public NewShortcut(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect the (optional) ComputerName, Username and Password parameters and Verbose and WhatIf flags
            base.Execute();

            // Obtain cmdlet parameters
            // Will contain all of the arguments from the 'ArgumentList Arguments' below
            string path = _arguments.Get<StringArgument>("Path").Value;
            string targetPath = _arguments.Get<StringArgument>("TargetPath").Value;
            string arguments = _arguments.Get<StringArgument>("Arguments").Value;
            string workingDirectory = _arguments.Get<StringArgument>("WorkingDirectory").Value;
            string hotkey = _arguments.Get<StringArgument>("Hotkey").Value;
            string iconLocation = _arguments.Get<StringArgument>("IconLocation").Value;
            string windowStyle = _arguments.Get<StringArgument>("WindowStyle").Value;
            bool force = _arguments.Get<BoolArgument>("Force").Value;

            // Validate arguments
            // Shortcut path
            if (File.Exists(path))
            {
                Console.Write($"File '{path}' already exists. ");

                if (!force)
                {
                    Console.WriteLine("Use -Force to replace it.");
                    return _results;
                }
                else
                {
                    Console.WriteLine("It will be replaced.");
                }
            }

            // Target path
            if (!File.Exists(targetPath))
                throw new NoPowerShellException($"Target path '{targetPath}' does not exist.");

            // Window style
            Dictionary<int, string> windowStyles = new Dictionary<int, string>()
            {
                { 1, "Normal" },
                { 3, "Maximized" },
                { 7, "Minimized" }
            };
            int windowStyleInt = 4;
            switch (windowStyle.ToLower())
            {
                case "normal":
                case "4":
                    windowStyleInt = 1;
                    break;
                case "maximized":
                case "3":
                    windowStyleInt = 3;
                    break;
                case "minimized":
                case "7":
                    windowStyleInt = 7;
                    break;
                default:
                    throw new NoPowerShellException($"Window style '{windowStyle}' is not valid. Valid values are 'Normal' (4), 'Maximized' (3) and 'Minimized' (7).");
            }

            // Icon location
            if (string.IsNullOrEmpty(iconLocation))
                iconLocation = string.Format("{0},0", targetPath);

            // Initialize COM object
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(shellType);

            // Create shortcut
            var shortcut = shell.CreateShortcut(path);
            shortcut.TargetPath = targetPath;
            shortcut.Arguments = arguments;
            shortcut.IconLocation = iconLocation;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.WindowStyle = windowStyleInt;
            shortcut.Hotkey = hotkey;
            shortcut.Save();

            _results.Add(
                new ResultRecord()
                {
                    { "Path", path },
                    { "TargetPath", shortcut.TargetPath },
                    { "Arguments", shortcut.Arguments },
                    { "IconLocation", shortcut.IconLocation },
                    { "WorkingDirectory", shortcut.WorkingDirectory },
                    { "WindowStyle", windowStyles[shortcut.WindowStyle] },
                    { "Hotkey", shortcut.Hotkey }
                }
            );

            // Return properties of newly created shorcut
            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "New-Shortcut" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("TargetPath"),
                    new StringArgument("Arguments", string.Empty, true),
                    new StringArgument("IconLocation", string.Empty, true),
                    new StringArgument("WorkingDirectory", string.Empty, true),
                    new StringArgument("WindowStyle", "Normal", true),
                    new StringArgument("Hotkey", string.Empty, true),
                    new BoolArgument("Force", false)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Create a new shortcut."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Create basic shortcut", @"New-Shortcut -Path C:\Users\Public\Desktop\Notepad.lnk -TargetPath C:\Windows\notepad.exe"),
                    new ExampleEntry("Create advanced shortcut", @"New-Shortcut -Path ""C:\Users\User1\Desktop\Microsoft Edge.lnk"" -TargetPath C:\Windows\System32\cmd.exe -Arguments ""/C echo PWNED>pwned.txt"" -IconLocation ""C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe,0"" -WorkingDirectory ""%~dp0"" -WindowStyle Minimized -Hotkey ""Ctrl+Shift+C"" -Force"),
                };
            }
        }
    }
}
