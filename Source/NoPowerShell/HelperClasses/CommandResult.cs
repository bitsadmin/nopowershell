using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class CommandResult : List<ResultRecord>
    {
        public enum OutputType { List, Table, Auto };
        public OutputType Output { get; set; }

        public CommandResult(int capacity) : base(capacity)
        {
            Output = OutputType.Auto;
        }

        public CommandResult() : base()
        {
            Output = OutputType.Auto;
        }
    }
}
