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
        private OutputType _output;

        public CommandResult(int capacity) : base(capacity)
        {
            _output = OutputType.Auto;
        }

        public CommandResult() : base()
        {
            _output = OutputType.Auto;
        }

        public OutputType Output
        {
            get { return _output; }
            set { _output = value; }
        }
    }
}
