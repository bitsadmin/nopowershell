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

        public override bool Equals(object obj)
        {
            // If parameter is null or an incorrect type, it for sure isn't equal
            if (!(obj is Dictionary<string, string> dict2))
                return false;

            // If count of columns is different, it isn't equal
            if (this.Count != dict2.Count)
                return false;

            // Validate values in all attributes
            foreach (KeyValuePair<string, string> kv1 in this)
            {
                // If attribute does not exist in second object
                if (!dict2.ContainsKey(kv1.Key))
                    return false;

                // If attribute value does not match the value in the second object
                if (dict2[kv1.Key] != kv1.Value)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
