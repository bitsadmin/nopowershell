using System;
using System.Collections.Generic;
using System.Management;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    class WmiHelper
    {
        public static CommandResult ExecuteWmiQuery(string wmiQuery)
        {
            return ExecuteWmiQuery(@"ROOT\CIMV2", wmiQuery);
        }

        public static CommandResult ExecuteWmiQuery(string wmiNamespace, string wmiQuery)
        {
            return ExecuteWmiQuery(wmiNamespace, wmiQuery, ".", null, null);
        }

        public static CommandResult ExecuteWmiQuery(string wmiNamespace, string wmiQuery, string computerName, string username, string password)
        {
            CommandResult queryResults = null;

            ManagementScope scope = GetScope(wmiNamespace, computerName, username, password);

            ObjectQuery query = new ObjectQuery(wmiQuery);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

            ManagementObjectCollection queryCollection = searcher.Get();
            Dictionary<string, int> columns = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            queryResults = new CommandResult(queryCollection.Count);

            // Determine column order
            int start = wmiQuery.ToLower().IndexOf("select") + "select".Length;
            int length = wmiQuery.ToLower().IndexOf(" from") - start;
            string columns_string = wmiQuery.Substring(start, length);
            string[] dirty_columns = columns_string.Split(',');
            foreach (string col in dirty_columns)
                columns.Add(col.Trim(), col.Trim().Length);

            // Case of SELECT *
            bool wildCardSelect = false;
            if (columns.ContainsKey("*"))
            {
                columns = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                wildCardSelect = true;
            }

            // Collect data
            foreach (ManagementObject m in queryCollection)
            {
                ResultRecord result = new ResultRecord(m.Properties.Count, StringComparer.InvariantCultureIgnoreCase);

                // Case of SELECT *
                if (wildCardSelect)
                {
                    foreach (PropertyData data in m.Properties)
                    {
                        columns.Add(data.Name, data.Name.Length);
                    }
                    wildCardSelect = false;
                }

                // Prepare order of columns
                foreach (string column in columns.Keys)
                    result.Add(column, null);

                // Collect attributes
                foreach (PropertyData data in m.Properties)
                {
                    string key = data.Name;
                    string value = string.Empty;
                    if (data.Value != null)
                    {
                        if (data.Value.GetType() == typeof(string[]))
                            value = string.Join(", ", (string[])data.Value);
                        else
                            value = Convert.ToString(data.Value);
                    }

                    result[key] = value;
                }

                queryResults.Add(result);
            }

            return queryResults;
        }

        public static CommandResult InvokeWmiMethod(string wmiNamespace, string wmiClass, string methodName, string methodArguments, string computerName, string username, string password)
        {
            CommandResult invokeResults = new CommandResult(1);

            ManagementScope scope = GetScope(wmiNamespace, computerName, username, password);
            ManagementClass mgmtClass = new ManagementClass(scope.Path.Path, wmiClass, null);
            MethodData method = mgmtClass.Methods[methodName];

            // -1 because ReturnValue does not count
            int paramCount = method.InParameters.Properties.Count + method.OutParameters.Properties.Count - 1;

            // Invoke the method
            object[] methodArgs = new object[paramCount];
            methodArgs[0] = methodArguments; // TODO, it should be possible to provide more arguments
            object returnValue = mgmtClass.InvokeMethod(methodName, methodArgs);

            // Store the ReturnValue
            ResultRecord outParams = new ResultRecord();
            outParams.Add("ReturnValue", Convert.ToString(returnValue));

            // Store other outParams
            int i = method.InParameters.Properties.Count;
            foreach (PropertyData param in method.OutParameters.Properties)
            {
                // ReturnValue is not stored in the methodarguments,
                // but instead is just the return value of the InvokeMethod method
                if (param.Name == "ReturnValue")
                    continue;

                outParams.Add(param.Name, Convert.ToString(methodArgs[i]));
                i++;
            }

            invokeResults.Add(outParams);

            return invokeResults;
        }

        private static ManagementScope GetScope(string wmiNamespace, string computerName, string username, string password)
        {
            ConnectionOptions options = new ConnectionOptions()
            {
                Impersonation = ImpersonationLevel.Impersonate,
                Username = username,
                Password = password
            };
            ManagementScope scope = new ManagementScope(string.Format(@"\\{0}\{1}", computerName, wmiNamespace), options);
            scope.Connect();

            return scope;
        }
    }
}
