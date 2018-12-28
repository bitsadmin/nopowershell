using System;
using Microsoft.Win32;

namespace NoPowerShell.HelperClasses
{
    class ProviderHelper
    {
        public static RegistryKey GetRegistryKey(ref string path)
        {
            RegistryKey root = null;
            path = path.ToUpperInvariant();

            if (path.StartsWith("HKLM:"))
            {
                root = Registry.LocalMachine;
                path = path.Replace("HKLM:", string.Empty);
            }
            else if (path.StartsWith("HKCU:"))
            {
                root = Registry.CurrentUser;
                path = path.Replace("HKCU:", string.Empty);
            }
            else if (path.StartsWith("HKCR:"))
            {
                root = Registry.ClassesRoot;
                path = path.Replace("HKCR:", string.Empty);
            }
            else if (path.StartsWith("HKU:"))
            {
                root = Registry.Users;
                path = path.Replace("HKU:", string.Empty);
            }

            if (root != null && path.StartsWith(@"\"))
                path = path.Substring(1);

            return root;
        }
    }
}
