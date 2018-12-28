using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class ResultRecord : Dictionary<string, string>
    {
        public ResultRecord() : base()
        {
        }

        public ResultRecord(int capacity) : base(capacity, StringComparer.InvariantCultureIgnoreCase)
        {
        }
    }
}
