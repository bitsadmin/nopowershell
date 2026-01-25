using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Management
{
    public class GetProcessCommand : PSCommand
    {
        public GetProcessCommand(string[] userArguments) : base(userArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            // Collect parameters for remote execution
            string computername = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            string allNameArguments = _arguments.Get<StringArgument>("Name").Value;

            string where = string.Empty;
            if (!string.IsNullOrEmpty(allNameArguments))
            {
                string[] processNames = allNameArguments.Split(',');
                StringBuilder whereStr = new StringBuilder(" Where (");

                bool first = true;
                foreach (string name in processNames)
                {
                    string newname = name;
                    if (!name.ToUpperInvariant().EndsWith(".EXE"))
                        newname = name + ".exe";

                    if (first)
                    {
                        whereStr.AppendFormat("(name = '{0}')", newname);
                        first = false;
                    }
                    else
                    {
                        whereStr.AppendFormat(" or (name = '{0}')", newname);
                    }
                }

                whereStr.Append(")");
                where = whereStr.ToString();
            }

            _results = WmiHelper.ExecuteWmiQuery("Select ProcessId, Name, CommandLine From Win32_Process" + where, computername, username, password);

            if(_results.Count == 0)
            {
                throw new NoPowerShellException("Cannot find a process with the name \"{0}\". Verify the process name and call the cmdlet again.", allNameArguments);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases => new CaseInsensitiveList()
        {
            "Get-Process",
            "ps"
        };

        public static new ArgumentList SupportedArguments => new ArgumentList()
        {
            new StringArgument("ComputerName", true),
            new StringArgument("Username", true),
            new StringArgument("Password", true),
            new StringArgument("Name", string.Empty)
        };

        public static new string Synopsis => "Gets the processes that are running on the local computer or a remote computer.";

        public static new ExampleEntries Examples => new ExampleEntries()
        {
            new ExampleEntry
            (
                "List processes",
                new List<string>()
                {
                    "Get-Process",
                    "ps"
                }
            ),
            new ExampleEntry
            (
                "List processes on remote host using WMI",
                new List<string>()
                {
                    "Get-Process -ComputerName MyServer -Username MyUser -Password MyPassword",
                    "ps -ComputerName MyServer"
                }
            )
        };
    }
}
