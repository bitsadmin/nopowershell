using Microsoft.Win32;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    class RegistryHelper
    {
        public static RegistryHive GetRoot(ref string path)
        {
            RegistryHive root;

            path = path.ToUpperInvariant();
            if (path.StartsWith("HKLM:"))
            {
                root = RegistryHive.LocalMachine;
                path = path.Replace("HKLM:", string.Empty);
            }
            else if (path.StartsWith("HKCU:"))
            {
                root = RegistryHive.CurrentUser;
                path = path.Replace("HKCU:", string.Empty);
            }
            else if (path.StartsWith("HKCR:"))
            {
                root = RegistryHive.ClassesRoot;
                path = path.Replace("HKCR:", string.Empty);
            }
            else if (path.StartsWith("HKU:"))
            {
                root = RegistryHive.Users;
                path = path.Replace("HKU:", string.Empty);
            }
            else
                throw new NoPowerShellException("Unknown registry path: \"{0}\"", path);

            if (path.StartsWith(@"\"))
                path = path.Substring(1);

            return root;
        }

        public static bool IsRegistryPath(string path)
        {
            string checkPath = path.ToUpperInvariant();

            return checkPath.StartsWith("HKLM:") || checkPath.StartsWith("HKCU:") || checkPath.StartsWith("HKCR:") || checkPath.StartsWith("HKU:");
        }
    }
}
