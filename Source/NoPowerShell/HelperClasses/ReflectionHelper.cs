using NoPowerShell.Commands;
using System;
using System.Collections.Generic;
using System.Reflection;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    class ReflectionHelper
    {
        /// <summary>
        /// Using reflection determine available commands
        /// </summary>
        /// <returns>List of commands</returns>
        public static Dictionary<Type, CaseInsensitiveList> GetCommands()
        {
            Dictionary<Type, CaseInsensitiveList> commandTypes = new Dictionary<Type, CaseInsensitiveList>();
            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.BaseType == typeof(PSCommand))
                {
                    CaseInsensitiveList aliases = null;
                    PropertyInfo aliasProperty = t.GetProperty("Aliases", BindingFlags.Static | BindingFlags.Public);
                    if (aliasProperty != null)
                        aliases = (CaseInsensitiveList)aliasProperty.GetValue(null, null);
                    else
                        aliases = new CaseInsensitiveList();

                    commandTypes.Add(t, aliases);
                }
            }

            return commandTypes;
        }
    }
}
