using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Data.SqlClient;
using System.Linq;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.SQLPS
{
    public class InvokeSqlcmdCommand : PSCommand
    {
        public InvokeSqlcmdCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            base.Execute();

            // Obtain cmdlet parameters
            string query = _arguments.Get<StringArgument>("Query").Value;
            string serverInstance = _arguments.Get<StringArgument>("serverInstance").Value;
            string database = _arguments.Get<StringArgument>("Database").Value;
            int timeout = _arguments.Get<IntegerArgument>("Timeout").Value;
            bool encryptConnection = _arguments.Get<BoolArgument>("EncryptConnection").Value;
            string connectionString = _arguments.Get<StringArgument>("ConnectionString").Value;

            // Build connection string
            connectionString = BuildConnectionString(connectionString, serverInstance, database, timeout, encryptConnection, username, password);

            // Establish connection
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                try
                {
                    // Display number of affected rows for INSERT and UPDATE statements
                    string[] commands = new string[] { "INSERT", "DELETE", "UPDATE" };
                    if (commands.Any(c => query.StartsWith(c, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        int affectedRows = cmd.ExecuteNonQuery();
                        Console.WriteLine("{0} row(s) affected\r\nThe command(s) completed successfully.", affectedRows);
                    }
                    // All other queries
                    else
                    {
                        SqlDataReader reader = cmd.ExecuteReader();

                        // Read the results and add them to the output
                        while (reader.Read())
                        {
                            ResultRecord row = new ResultRecord();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i).ToString();
                            }
                            _results.Add(row);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new NoPowerShellException(ex.Message);
                }
            }

            // Return results
            return _results;
        }

        private static string BuildConnectionString(string connectionString, string serverInstance, string database, int timeout, bool encryptConnection, string username, string password)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

            // Server instance
            builder.DataSource = serverInstance;

            // Connection timeout
            builder.ConnectTimeout = timeout;

            // Database
            if (!string.IsNullOrEmpty(database))
            {
                builder.InitialCatalog = database;
            }

            // Authentication
            if (!string.IsNullOrEmpty(username))
            {
                builder.UserID = username;
                builder.Password = password;
                builder.IntegratedSecurity = false;
            }
            else
            {
                builder.IntegratedSecurity = true;
            }

            // Encrypt connection
            builder.Encrypt = encryptConnection;

            return builder.ConnectionString;
        }

        public static new CaseInsensitiveList Aliases
        {
            get
            {
                return new CaseInsensitiveList() {
                    "Invoke-Sqlcmd",
                    "sqlcmd" // unofficial
                };
            }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Query"),
                    new StringArgument("ServerInstance", "."),
                    new StringArgument("Database", true),
                    new IntegerArgument("Timeout", 30),
                    new BoolArgument("EncryptConnection"),
                    new StringArgument("ConnectionString", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Runs a script containing statements from the languages (Transact-SQL and XQuery) and commands supported by the SQL Server sqlcmd utility."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry("List SQL Server version", "Invoke-Sqlcmd -Query \"SELECT @@version\""),
                    new ExampleEntry("Query specific server", "Invoke-Sqlcmd -Query \"SELECT username,password FROM users\" -ServerInstance SQL1"),
                    new ExampleEntry("Use explicit authentication", "Invoke-Sqlcmd -Query \"exec xp_cmdshell 'dir C:\\'\" -ServerInstance SQL1 -Username sa -password Password1!"),
                    new ExampleEntry("Use encrypted connection", "Invoke-Sqlcmd -Query \"INSERT INTO logins (username,password) VALUES ('newuser', 'MyPass')\" -ServerInstance SQL1 -Database CRM -EncryptConnection"),
                    new ExampleEntry("Use connectionstring to named pipe", "Invoke-Sqlcmd -Query \"SELECT * FROM transactions LIMIT 10'\" -ConnectionString \"Server=\\\\.\\pipe\\sql\\query; Database=Sales; Integrated Security=True;\""),
                };
            }
        }
    }
}
