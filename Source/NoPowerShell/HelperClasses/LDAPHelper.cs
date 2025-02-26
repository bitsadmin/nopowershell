using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    class LDAPHelper
    {
        public static string GetDistinguishedName(string server, string username, string password)
        {
            CommandResult result = LDAPHelper.QueryLDAP(null, SearchScope.Base, "(objectClass=*)", new List<string>() { "distinguishedname" }, 1, server, username, password);
            
            if(result.Count == 0)
                return string.Empty;

            return result[0]["distinguishedname"];
        }

        public static CommandResult QueryLDAP(string queryFilter, List<string> properties)
        {
            return QueryLDAP(null, queryFilter, properties, null, null, null);
        }

        public static CommandResult QueryLDAP(string searchBase, string queryFilter, List<string> properties, string server, string username, string password)
        {
            return QueryLDAP(searchBase, SearchScope.Subtree, queryFilter, properties, 0, server, username, password);
        }

        public static CommandResult QueryLDAP(string searchBase, SearchScope scope, string queryFilter, List<string> properties, int resultSetSize, string server, string username, string password)
        {
            CommandResult _results = new CommandResult();

            // Select all properties if * parameter is provided
            if (properties == null || (properties.Count > 0 && properties[0] == "*"))
                properties = new List<string>(0);

            // Compile LDAP connection string
            string ldap = "LDAP://";
            bool hasServer = !string.IsNullOrEmpty(server);
            bool hasSearchBase = !string.IsNullOrEmpty(searchBase);
            if (hasServer)
            {
                ldap += server;
                
                if (hasSearchBase)
                    ldap += "/" + searchBase;
            }
            else if (hasSearchBase)
                ldap += searchBase;
            if (ldap == "LDAP://")
                ldap = string.Empty;

            // Initialize LDAP
            DirectoryEntry entry = new DirectoryEntry(
                ldap,
                username,
                password,
                AuthenticationTypes.Secure
            );

            // Initialize searcher
            using (DirectorySearcher ds = new DirectorySearcher(entry))
            {
                // Setup properties
                ds.PropertiesToLoad.Clear();
                ds.PropertiesToLoad.AddRange(properties.ToArray());
                ds.SearchScope = scope;
                ds.SizeLimit = resultSetSize;
                ds.SecurityMasks = SecurityMasks.Dacl | SecurityMasks.Group | SecurityMasks.Owner; // Required to obtain nTSecurityDescriptor

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
                            // Convert byte array field to SDDL
                            case "ntsecuritydescriptor":
                            case "msds-allowedtoactonbehalfofotheridentity":
                                RawSecurityDescriptor descriptor = new RawSecurityDescriptor((byte[])objArray[0], 0);
                                foundRecord[propertyKey] = descriptor.GetSddlForm(AccessControlSections.All);
                                //ActiveDirectorySecurity descriptor = new ActiveDirectorySecurity();
                                //descriptor.SetSecurityDescriptorBinaryForm((byte[])objArray[0], AccessControlSections.All);
                                continue;
                            // Date fields
                            case "lastlogon":
                            case "lastlogontimestamp":
                            case "badpasswordtime":
                            case "pwdlastset":
                            case "passwordlastset":
                            case "usnchanged":
                            case "usncreated":
                                DateTime lastlogon = DateTime.FromFileTime((long)objArray[0]);
                                foundRecord[propertyKey] = lastlogon.ToFormattedString();
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
