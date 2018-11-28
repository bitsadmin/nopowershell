using System.Collections.Generic;

/*
Author: @_bitsadmin
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

        public ResultRecord(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer)
        {
        }
    }
}
