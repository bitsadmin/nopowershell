using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class ResultRecord : Dictionary<string, string>, ICloneable
    {
        public ResultRecord() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public ResultRecord(int capacity) : base(capacity, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        public ResultRecord(Dictionary<string, string> dictionary) : base(dictionary, StringComparer.InvariantCultureIgnoreCase)
        {
        }

        public object Clone()
        {
            return new ResultRecord(this);
        }
    }
}
