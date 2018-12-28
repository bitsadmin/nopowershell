using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class GetProcessCommand : PSCommand
    {
        public GetProcessCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
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

            // Remote
            string computerName = _arguments.Get<StringArgument>("ComputerName").Value;
            string username = _arguments.Get<StringArgument>("Username").Value;
            string password = _arguments.Get<StringArgument>("Password").Value;

            _results = WmiHelper.ExecuteWmiQuery(@"ROOT\CIMV2", "Select ProcessId, Name, CommandLine From Win32_Process" + where, computerName, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-Process", "ps" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Name", string.Empty)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets the processes that are running on the local computer or a remote computer."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List processes", "Get-Process"),
                    new ExampleEntry("List processes on remote host", "Get-Process -ComputerName dc01.corp.local -Username Administrator -Password P4ssw0rd!")
                };
            }
        }
    }
}
