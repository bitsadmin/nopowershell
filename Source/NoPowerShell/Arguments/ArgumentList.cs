using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Arguments
{
    public class ArgumentList : List<Argument>
    {
        public T Get<T>(string argumentName) where T : Argument
        {
            foreach (Argument arg in this)
            {
                // Skip irrelevant arguments
                if (arg.GetType() != typeof(T))
                    continue;

                // Check for matching argumentName
                T foundArg = arg as T;
                if (foundArg.Name.Equals(argumentName, StringComparison.InvariantCultureIgnoreCase))
                    return foundArg;
            }

            return null;
        }
    }
}
