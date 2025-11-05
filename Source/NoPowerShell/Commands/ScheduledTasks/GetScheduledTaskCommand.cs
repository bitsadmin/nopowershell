using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/
namespace NoPowerShell.Commands
{
    public class GetScheduledTaskCommand : PSCommand
    {
        public GetScheduledTaskCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }
        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Collect parameters
            base.Execute();
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            string taskNameStr = _arguments.Get<StringArgument>("TaskName").Value;
            string taskPathStr = _arguments.Get<StringArgument>("TaskPath").Value;
            string[] taskNames = string.IsNullOrEmpty(taskNameStr) ? new string[0] : taskNameStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] taskPaths = string.IsNullOrEmpty(taskPathStr) ? new string[0] : taskPathStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Initiate COM object
            Type tsType = Type.GetTypeFromProgID("Schedule.Service");
            dynamic ts = Activator.CreateInstance(tsType);
            if (computerName == "." || string.IsNullOrEmpty(computerName))
                ts.Connect();
            else
                ts.Connect(computerName);

            // Collect details of tasks
            dynamic rootFolder = ts.GetFolder(@"\");
            List<dynamic> allTasks = GetTasksRecursive(rootFolder);
            List<TaskInfo> filteredTasks = new List<TaskInfo>();
            foreach (dynamic task in allTasks)
            {
                // Fetch values
                bool enabled = task.Enabled;
                DateTime lastRunTime = task.LastRunTime;
                int lastTaskResult = task.LastTaskResult;
                DateTime nextRunTime = task.NextRunTime;
                int numberOfMissedRuns = task.NumberOfMissedRuns;
                string xml = task.Xml;
                string fullPath = task.Path;
                string name = task.Name;
                string path = fullPath.Substring(0, fullPath.Length - name.Length);

                // Check if match
                bool pathMatch = taskPaths.Length == 0 || MatchesAnyWildcard(path, taskPaths);
                bool nameMatch = taskNames.Length == 0 || MatchesAnyWildcard(name, taskNames);

                // Add to list of tasks if match
                if (pathMatch && nameMatch)
                {
                    string state = TaskStateToString(task.State);
                    filteredTasks.Add(
                        new TaskInfo {
                            TaskPath = path,
                            TaskName = name,
                            State = state,
                            LastRunTime = lastRunTime,
                            LastTaskResult = lastTaskResult,
                            NextRunTime = nextRunTime,
                            NumberOfMissedRuns = numberOfMissedRuns,
                            Xml = xml
                        }
                    );
                }
            }

            // Sort tasks by path
            filteredTasks.Sort((t1, t2) =>
            {
                int pathCompare = string.Compare(t1.TaskPath, t2.TaskPath, StringComparison.OrdinalIgnoreCase);
                if (pathCompare != 0)
                    return pathCompare;
                return string.Compare(t1.TaskName, t2.TaskName, StringComparison.OrdinalIgnoreCase);
            });

            // Collect relevant details
            foreach (TaskInfo taskInfo in filteredTasks)
            {
                _results.Add(
                    new ResultRecord()
                    {
                        { "TaskPath", taskInfo.TaskPath },
                        { "TaskName", taskInfo.TaskName },
                        { "State", taskInfo.State },
                        { "LastRunTime", taskInfo.LastRunTime.ToFormattedString() },
                        { "LastTaskResult", taskInfo.LastTaskResult.ToString() },
                        { "NextRunTime", taskInfo.NextRunTime.ToFormattedString() },
                        { "NumberOfMissedRuns", taskInfo.NumberOfMissedRuns.ToString() },
                        { "Xml", taskInfo.Xml }
                    }
                );
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ScheduledTask", "schtasks" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("TaskName", true),
                    new StringArgument("TaskPath", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the task definitions that are registered in the Task Scheduler service."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("Get all scheduled tasks from the root folder and subfolders", "Get-ScheduledTask | select TaskPath,TaskName,Status"),
                    new ExampleEntry("Get all scheduled tasks from a specific folder", "Get-ScheduledTask -TaskPath \"\\Microsoft\\Windows\\Windows Defender\\*\" | select TaskPath,TaskName,Status"),
                    new ExampleEntry("Get a specific scheduled task", "Get-ScheduledTask -TaskName \"Windows Defender Scheduled Scan\""),
                    new ExampleEntry("Get scheduled tasks matching a wildcard", "Get-ScheduledTask -TaskName Update* | select TaskPath,TaskName,Status"),
                    new ExampleEntry("Get scheduled tasks on a remote computer", "Get-ScheduledTask -ComputerName MyServer | select TaskPath,TaskName,Status")
                };
            }
        }

        private class TaskInfo
        {
            public string TaskPath { get; set; }
            public string TaskName { get; set; }
            public string State { get; set; }
            public bool Enabled { get; set; }
            public DateTime LastRunTime { get; set; }
            public int LastTaskResult { get; set; }
            public DateTime NextRunTime { get; set; }
            public int NumberOfMissedRuns { get; set; }
            public string Xml { get; set; }
        }
        private List<dynamic> GetTasksRecursive(dynamic folder)
        {
            List<dynamic> tasksList = new List<dynamic>();
            dynamic tasks = folder.GetTasks(1);
            foreach (dynamic task in tasks)
            {
                tasksList.Add(task);
            }
            dynamic subFolders = folder.GetFolders(0);
            foreach (dynamic subFolder in subFolders)
            {
                tasksList.AddRange(GetTasksRecursive(subFolder));
            }
            return tasksList;
        }

        private bool MatchesAnyWildcard(string text, string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                string trimmed = pattern.Trim();
                if (WildcardMatch(text, trimmed))
                    return true;
            }
            return false;
        }

        private bool WildcardMatch(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;
            string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }

        private string TaskStateToString(int state)
        {
            switch (state)
            {
                case 0: return "Unknown";
                case 1: return "Disabled";
                case 2: return "Queued";
                case 3: return "Ready";
                case 4: return "Running";
                default: return "Unknown";
            }
        }
    }
}