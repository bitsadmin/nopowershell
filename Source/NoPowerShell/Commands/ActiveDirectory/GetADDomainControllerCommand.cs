using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.ActiveDirectory
{
    public class GetADDomainControllerCommand : PSCommand
    {
        public GetADDomainControllerCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        /*
        distinguishedName
        [DefaultPartition]
        [Domain]
        Enabled (userAccountControl)
        [Forest]
        dNSHostName
        [InvocationId]
        [IPv4Address]
        [IPv6Address]
        [IsGlobalCatalog]
        [IsReadOnly]primaryGroupID != 521
        [LdapPort]
        [NTDSSettingsObjectDN] = "CN=NTDS Settings,CN={0},CN=Servers,CN=Global,CN=Sites,CN=Configuration,{1}", cn, searchBase
        operatingSystem
        [OperatingSystemHotfix]
        [OperatingSystemServicePack]
        operatingSystemVersion
        [OperationMasterRoles] = CN=NTDS Settings,CN=DC1,CN=Servers,CN=Global,CN=Sites,CN=Configuration,DC=ad,DC=bitsadmin,DC=com -> msDS-hasMasterNCs
        [Partitions]
        [ServerObjectDN] = "CN={0},CN=Servers,CN=Global,CN=Sites,CN=Configuration,{1}", cn
        [ServerObjectGuid] = [ServerObjectDN] -> objectGUID[0]
        [Site] = Global
        [SslPort] = 636
        */
        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain Username/Password parameters
            base.Execute(pipeIn);

            // Obtain cmdlet parameters
            string server = _arguments.Get<StringArgument>("Server").Value;
            string filter = _arguments.Get<StringArgument>("Filter").Value;

            string dn = LDAPHelper.GetDistinguishedName(server, username, password);

            // Build filter
            string queryFilter = "(primaryGroupID=516)"; // Only DCs
            if (filter == "*")
                queryFilter = "(|(primaryGroupID=516)(primaryGroupID=521))"; // Also RODCs
            else if (!string.IsNullOrEmpty(filter))
                throw new NoPowerShellException("Currently only * filter is supported");

            // Query for DC(s)
            List<string> properties = new List<string>() { "distinguishedName", "userAccountControl", "dNSHostName", "primaryGroupID", "operatingSystem", "operatingSystemVersion" };
            _results = LDAPHelper.QueryLDAP(dn, queryFilter, properties, server, username, password);

            // Obtain forest
            string enterpriseConfig = $"CN=Enterprise Configuration,CN=Partitions,CN=Configuration,{dn}";
            CommandResult dnsRoot = LDAPHelper.QueryLDAP(dn, enterpriseConfig, new List<string>() { "dnsRoot" }, server, username, password);
            
            // Obtain 

            // Obtain OperationMasterRoles
            List<string> props = new List<string>() { };
            LDAPHelper.QueryLDAP(dn, queryFilter, props, server, username, password);

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-ADDomainController" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Server", true),
                    new StringArgument("Filter", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets one or more Active Directory computers."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List domain controllers", "Get-ADDomainController"),
                    new ExampleEntry("List all domain controllers, including read-only ones", "Get-ADDomainController -Filter *")
                };
            }
        }
    }
}
