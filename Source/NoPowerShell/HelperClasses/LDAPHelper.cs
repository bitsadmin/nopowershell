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
            return QueryLDAP(null, queryFilter, properties, null, null, null);
        }

        public static CommandResult QueryLDAP(string searchBase, string queryFilter, List<string> properties, string server, string username, string password)
        {
            return QueryLDAP(searchBase, SearchScope.Subtree, queryFilter, properties, server, username, password);
        }

        public static CommandResult QueryLDAP(string searchBase, SearchScope scope, string queryFilter, List<string> properties, string server, string username, string password)
        {
            CommandResult _results = new CommandResult();

            // Select all properties if * parameter is provided
            if (properties == null || (properties.Count > 0 && properties[0] == "*"))
                properties = new List<string>(0);

            // Compile LDAP connection string
            string ldap = "LDAP://";
            if(!string.IsNullOrEmpty(server))
                ldap += server;
            if (!string.IsNullOrEmpty(searchBase))
                ldap += "/" + searchBase;
            if (ldap == "LDAP://")
                ldap = string.Empty;

            // Initialize LDAP
            DirectoryEntry entry = new DirectoryEntry(
                ldap,
                username,
                password
            );

            // Initialize searcher
            using (DirectorySearcher ds = new DirectorySearcher(entry))
            {
                // Setup properties
                ds.PropertiesToLoad.Clear();
                ds.PropertiesToLoad.AddRange(properties.ToArray());
                ds.SearchScope = scope;

                // Filter
                ds.Filter = queryFilter;

                // Perform query
                SearchResultCollection results = ds.FindAll();

                // Validate attributes
                ResultRecord recordTemplate = null;
                if (results.Count > 0)
                {
                    SearchResult firstResult = results[0];

                    // Add all available properties to a case-insensitive list
                    CaseInsensitiveList ciProperties = new CaseInsensitiveList();
                    foreach (DictionaryEntry prop in firstResult.Properties)
                        ciProperties.Add((string)prop.Key);

                    // Validate if all properties are available
                    //foreach (string property in properties)
                    //    if (!ciProperties.Contains(property))
                    //        throw new NoPowerShellException(string.Format("Column {0} not available in results", property));
                    
                    // Create template
                    // This makes sure the order of columns is in the order the user specified
                    recordTemplate = new ResultRecord(properties.Count);
                    foreach(string property in properties)
                        recordTemplate.Add(property, null);
                }

                // Store results
                for (int i = 0; i < results.Count; i++)
                {
                    SearchResult result = results[i];

                    // First records should have the same number of properties as any other record
                    ResultRecord foundRecord = (ResultRecord)recordTemplate.Clone();

                    // Iterate over result properties
                    foreach (DictionaryEntry property in result.Properties)
                    {
                        string propertyKey = property.Key.ToString();
                        ResultPropertyValueCollection objArray = (ResultPropertyValueCollection)property.Value;

                        // Fixups
                        switch (propertyKey.ToLowerInvariant())
                        {
                            // Byte array needs to be converted to GUID
                            case "objectguid":
                                Guid g = new Guid((byte[])objArray[0]);
                                foundRecord[propertyKey] = g.ToString();
                                continue;
                            // Byte array needs to be converted to SID
                            case "objectsid":
                            case "securityidentifier":
                                SecurityIdentifier sid = new SecurityIdentifier((byte[])objArray[0], 0);
                                foundRecord[propertyKey] = sid.ToString();
                                continue;
                            // Date fields
                            case "lastlogon":
                            case "lastlogontimestamp":
                            case "badpasswordtime":
                            case "pwdlastset":
                                DateTime lastlogon = DateTime.FromFileTime((long)objArray[0]);
                                foundRecord[propertyKey] = lastlogon.ToString();
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

                        // Store result
                        foundRecord[property.Key.ToString()] = strValue;
                    }

                    _results.Add(foundRecord);
                }
            }

            return _results;
        }

        public static bool IsActive(string bits)
        {
            if (string.IsNullOrEmpty(bits))
                return true;

            int flags = Convert.ToInt32(bits);

            return !Convert.ToBoolean(flags & 0x0002);
        }
    }
}
