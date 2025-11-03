using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADTrustCommand : PSCommand
    {
        public GetADTrustCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain Username/Password parameters
            base.Execute(pipeIn);

            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string properties = _arguments.Get<StringArgument>("Properties").Value;
            string ldapFilter = _arguments.Get<StringArgument>("LDAPFilter").Value;
            string filter = _arguments.Get<StringArgument>("Filter").Value;
            int maxDepth = _arguments.Get<IntegerArgument>("Depth").Value;

            // Determine properties
            List<string> arrProperties = null;
            if (!string.IsNullOrEmpty(properties))
                arrProperties = new List<string>(properties.Split(','));
            //else
            //    arrProperties = new List<string>() { "Name", "trustDirection", "securityIdentifier" };

            if (!string.IsNullOrEmpty(filter) && !string.IsNullOrEmpty(ldapFilter))
                throw new NoPowerShellException("Specify either Filter or LDAPFilter, not both");

            // Build LDAP filter
            if (!string.IsNullOrEmpty(ldapFilter))
                ldapFilter = string.Format("(&(objectClass=TrustedDomain){0})", ldapFilter);

            // Filter *
            if (filter == "*")
                ldapFilter = string.Empty;
            else if (!string.IsNullOrEmpty(filter))
                throw new NoPowerShellException("Currently only * filter is supported");

            // Perform query
            _results = GetTrustsRecursive(server, ldapFilter, arrProperties, 1, maxDepth);

            return _results;
        }

        private CommandResult GetTrustsRecursive(string server, string filter, List<string> properties, int depth, int maxDepth)
        {
            CommandResult results = new CommandResult();

            // Identify whether this is a custom query or just the regular one
            bool customQuery = false;
            string ldapFilter = null;

            // Regulary query
            if (string.IsNullOrEmpty(filter) && properties == null)
            {
                ldapFilter = "(objectClass=TrustedDomain)";
            }
            // Custom query
            else
            {
                customQuery = true;
                ldapFilter = filter;
            }

            // Perform LDAP query
            CommandResult domains;
            try
            {
                domains = LDAPHelper.QueryLDAP(null, ldapFilter, properties, server, username, password);
            }
            // Ignore if server returns an exception
            catch(System.Runtime.InteropServices.COMException)
            {
                return results;
            }

            // Parse results
            CommandResult subresults = new CommandResult();
            foreach (ResultRecord domain in domains)
            {
                TrustDirection direction = (TrustDirection)Convert.ToInt32(domain["trustDirection"]);

                // Regular output
                // List of domain trusts
                if (!customQuery)
                {
                    // Collect values
                    string name = domain["Name"];
                    string flatName = domain["flatName"];
                    string sid = domain["securityIdentifier"];

                    // Add result
                    results.Add(
                        new ResultRecord()
                        {
                            { "#", string.Empty },
                            { "Name", flatName },
                            { "Domain", name },
                            { "Parent", server },
                            { "Direction", direction.ToString() },
                            { "SID", sid }
                        }
                    );

                    // Skip recursion if maximum depth is reached
                    if (depth >= maxDepth)
                        continue;

                    // Follow outgoing trusts
                    if ((direction & TrustDirection.Outbound) == TrustDirection.Outbound)
                    {
                        CommandResult res = GetTrustsRecursive(name, filter, properties, depth + 1, maxDepth);
                        subresults.AddRange(res);
                    }
                }
                // Custom query has been performed
                // Show all properties that are returned
                else
                {
                    if (domain.ContainsKey("trustAttributes"))
                    {
                        // Obtain trustAttributes values
                        int trustAttributes = Convert.ToInt32(domain["trustAttributes"]);

                        // Store in ResultRecord
                        // Bits as specified at https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-adts/e9a2d23c-c31e-4a6f-88a0-6646fdb51a3c1
                        domain.Add("DisallowTransivity", IsBitSet(trustAttributes, 0).ToString());
                        domain.Add("UplevelOnly", IsBitSet(trustAttributes, 1).ToString());
                        domain.Add("ForestTransitive", IsBitSet(trustAttributes, 3).ToString());
                        domain.Add("UsesRC4Encryption", IsBitSet(trustAttributes, 7).ToString());
                        domain.Add("TGTDelegation", IsBitSet(trustAttributes, 9).ToString());
                    }

                    // TrustType
                    if (domain.ContainsKey("trustType"))
                    {
                        TrustType type = (TrustType)Convert.ToInt32(domain["trustType"]);
                        domain["TrustType"] = type.ToString();
                    }

                    if (domain.ContainsKey("DistinguishedName"))
                    {
                        string dn = domain["DistinguishedName"];
                        domain.Add("Source", string.Join(",", Array.FindAll(dn.Split(','), s => s.StartsWith("DC="))));
                    }

                    // Direction
                    domain.Add("Direction", direction.ToString());
                    

                    // Attributes to be added:
                    // - IntraForest (bool)
                    // - IsTreeParent (bool)
                    // - IsTreeRoot (bool)
                    // - SelectiveAuthentication (bool)
                    // - SIDFilteringForestAware (bool)
                    // - SIDFilteringQuarantined (bool)
                    // - Target (string)
                    // - TrustedPolicy (?)
                    // - TrustingPolicy (?)
                    // - UsesAESKeys (bool)

                    results.Add(domain);
                }
            }

            // Only sort and add indices in case of regular output
            if (!customQuery)
            {
                // Sort
                subresults.Sort((x, y) => x["Name"].CompareTo(y["Name"]));

                // Add subresults to current results
                results.AddRange(subresults);

                // Add indices
                int i = 0;
                foreach (ResultRecord r in results)
                {
                    r["#"] = i.ToString();
                    i++;
                }
            }

            return results;
        }

        private static bool IsBitSet(int b, int pos)
        {
            return ((b >> pos) & 1) != 0;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADTrust", "nltest" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true),
                    new StringArgument("Properties", true),
                    new StringArgument("Filter", true),
                    new StringArgument("LDAPFilter", true),
                    new IntegerArgument("Depth", 1) // Unofficial parameter
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Returns all trusted domain objects in the directory."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List all direct trusts", "Get-ADTrust -Filter *"),
                    new ExampleEntry("List trusts recursively till depth 3", "Get-ADTrust -Filter * -Depth 3"),
                    new ExampleEntry("List all details of a certain trust", "Get-ADTrust -LDAPFilter \"(Name=mydomain.com)\""),
                    new ExampleEntry("List specific details of a certain trust", "Get-ADTrust -LDAPFilter \"(Name=mydomain.com)\" -Properties Name,trustDirection,securityIdentifier")
                };
            }
        }
    }
}
