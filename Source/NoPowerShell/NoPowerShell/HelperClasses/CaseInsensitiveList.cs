using System;
using System.Collections.Generic;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class CaseInsensitiveList : List<string>
    {
        public new bool Contains(string item)
        {
            foreach (string s in this)
            {
                if (s.Equals(item, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
