using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using System.Text;

namespace NoPowerShell.HelperClasses
{
    class LDAPHelper
    {
        public static CommandResult QueryLDAP(string queryFilter, List<string> properties)
        {
            CommandResult _results = new CommandResult();

            DirectoryEntry entry = new DirectoryEntry();
            using (DirectorySearcher ds = new DirectorySearcher(entry))
            {
                // Setup properties
                ds.PropertiesToLoad.Clear();
                ds.PropertiesToLoad.AddRange(properties.ToArray());

                // Filter
                ds.Filter = queryFilter;

                // Perform query
                SearchResultCollection results = ds.FindAll();

                // Store results
                for (int i = 0; i < results.Count; i++)
                {
                    SearchResult result = results[i];

                    // First records should have the same number of properties as any other record
                    ResultRecord foundUser = new ResultRecord(results[0].Properties.Count);

                    // Iterate over result properties
                    foreach (DictionaryEntry property in result.Properties)
                    {
                        string propertyKey = property.Key.ToString();
                        ResultPropertyValueCollection objArray = (ResultPropertyValueCollection)property.Value;

                        // Fixups
                        switch (propertyKey.ToLowerInvariant())
                        {
                            // The UserAccountControl bitmask contains a bit which states whether the account is enabled or not
                            case "useraccountcontrol":
                                foundUser.Add("Enabled", IsActive(result).ToString());
                                continue;
                            // Byte array needs to be converted to GUID
                            case "objectguid":
                                Guid g = new Guid((byte[])objArray[0]);
                                foundUser.Add(propertyKey, g.ToString());
                                continue;
                            // Byte array needs to be converted to SID
                            case "objectsid":
                                SecurityIdentifier sid = new SecurityIdentifier((byte[])objArray[0], 0);
                                foundUser.Add(propertyKey, sid.ToString());
                                continue;
                            // This attribute is automatically added
                            case "adspath":
                                if (!properties.Contains(propertyKey))
                                    continue;
                                break;
                        }

                        // Convert objects to string
                        string[] strArray = new string[objArray.Count];
                        for (int j = 0; j < objArray.Count; j++)
                            strArray[j] = objArray[j].ToString();

                        // Concatenate strings
                        string strValue = string.Join(";", strArray);

                        foundUser.Add(property.Key.ToString(), strValue);
                    }

                    _results.Add(foundUser);
                }
            }

            return _results;
        }

        private static bool IsActive(SearchResult de)
        {
            if (de.Properties["ObjectGUID"] == null)
                return false;

            int flags = Convert.ToInt32(de.Properties["userAccountControl"][0]);

            return !Convert.ToBoolean(flags & 0x0002);
        }
    }
}
