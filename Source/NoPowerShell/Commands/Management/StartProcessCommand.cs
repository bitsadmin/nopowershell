using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;


/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class StartProcessCommand : PSCommand
    {
        public StartProcessCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            string filePath = _arguments.Get<StringArgument>("FilePath").Value;
            string arguments = _arguments.Get<StringArgument>("Arguments").Value;
            string verb = _arguments.Get<StringArgument>("Verb").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;
            string workingDirectory = _arguments.Get<StringArgument>("WorkingDirectory").Value;
            string windowStyleRaw = _arguments.Get<StringArgument>("WindowStyle").Value;

            ValidateArguments(verb, username, password);

            if (string.IsNullOrWhiteSpace(workingDirectory))
                workingDirectory = Environment.CurrentDirectory;

            ProcessWindowStyle windowStyle = ParseWindowStyle(windowStyleRaw);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = filePath;
            startInfo.Arguments = arguments ?? string.Empty;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.WindowStyle = windowStyle;

            if (string.Equals(verb, "runasuser", StringComparison.InvariantCultureIgnoreCase))
            {
                startInfo.UseShellExecute = false;
                SetCredentials(startInfo, username, password);
            }
            else
            {
                startInfo.UseShellExecute = true;
                if (!string.IsNullOrWhiteSpace(verb))
                    startInfo.Verb = verb;
            }

            try
            {
                Process startedProcess = Process.Start(startInfo);
                if (startedProcess == null)
                    throw new NoPowerShellException("Process did not start.");

                string processName = ResolveProcessName(startedProcess, filePath);

                _results.Add(
                    new ResultRecord()
                    {
                        { "Id", startedProcess.Id.ToString() },
                        { "ProcessName", processName },
                        { "FilePath", filePath }
                    }
                );
            }
            catch (NoPowerShellException)
            {
                throw;
            }
            catch (Win32Exception ex)
            {
                throw new NoPowerShellException("Cannot start process \"{0}\" because of the following error: {1}", filePath, ex.Message);
            }
            catch (Exception ex)
            {
                throw new NoPowerShellException("Cannot start process \"{0}\" because of the following error: {1}", filePath, ex.Message);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Start-Process",
            "start",
            "saps"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("FilePath"),
            new StringArgument("Arguments", true),
            new StringArgument("Verb", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            new StringArgument("WorkingDirectory", true),
            new StringArgument("WindowStyle", "Normal")
        };

        public static new string Synopsis => "Starts one or more processes on the local computer.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry("Start a process with positional FilePath and Arguments", "Start-Process C:\\test.exe \"Arg1 Arg2 Arg3\""),
            new ExampleEntry("Start a hidden process", "Start-Process -FilePath C:\\test.exe -Arguments \"Arg1 Arg2 Arg3\" -WindowStyle Hidden"),
            new ExampleEntry("Start process with elevated privileges", "Start-Process C:\\Tmp\\Legit.exe -Verb RunAs"),
            new ExampleEntry("Start process with alternate credentials", "Start-Process C:\\Tmp\\Legit.exe -WorkingDirectory C:\\Tmp -Verb RunAsUser -Username ad.bitsadmin.com\\User1 -Password MyPass")
        };

        private static void ValidateArguments(string verb, string username, string password)
        {
            bool hasUsername = !string.IsNullOrEmpty(username);
            bool hasPassword = !string.IsNullOrEmpty(password);

            if (hasUsername != hasPassword)
                throw new NoPowerShellException("Username and Password must be specified together.");

            bool isRunAsUser = string.Equals(verb, "runasuser", StringComparison.InvariantCultureIgnoreCase);
            if (isRunAsUser && (!hasUsername || !hasPassword))
                throw new NoPowerShellException("-Verb RunAsUser requires -Username and -Password.");

            if (!isRunAsUser && (hasUsername || hasPassword))
                throw new NoPowerShellException("Username and Password can only be used together with -Verb RunAsUser.");
        }

        private static ProcessWindowStyle ParseWindowStyle(string windowStyle)
        {
            if (string.IsNullOrWhiteSpace(windowStyle))
                return ProcessWindowStyle.Normal;

            switch (windowStyle.ToLowerInvariant())
            {
                case "normal":
                    return ProcessWindowStyle.Normal;
                case "hidden":
                    return ProcessWindowStyle.Hidden;
                case "minimized":
                    return ProcessWindowStyle.Minimized;
                case "maximized":
                    return ProcessWindowStyle.Maximized;
                default:
                    throw new NoPowerShellException("WindowStyle must be one of: Normal, Hidden, Minimized, Maximized.");
            }
        }

        private static void SetCredentials(ProcessStartInfo startInfo, string username, string password)
        {
            int slashIndex = username.IndexOf('\\');
            if (slashIndex > 0)
            {
                startInfo.Domain = username.Substring(0, slashIndex);
                startInfo.UserName = username.Substring(slashIndex + 1);
            }
            else
            {
                startInfo.UserName = username;
            }

            SecureString securePassword = new SecureString();
            foreach (char c in password)
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            startInfo.Password = securePassword;
        }

        private static string ResolveProcessName(Process startedProcess, string filePath)
        {
            try
            {
                return startedProcess.ProcessName;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(filePath);
            }
        }
    }
}