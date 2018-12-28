using System;
using System.Collections.Generic;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    public class ResultPrinter
    {
        public static void OutputResults(CommandResult results)
        {
            if (results == null)
                return;

            switch (results.Output)
            {
                case CommandResult.OutputType.List:
                    FormatList(results);
                    break;
                case CommandResult.OutputType.Table:
                    FormatTable(results);
                    break;
                default:
                    AutoFormat(results);
                    break;
            }
        }

        public static void AutoFormat(CommandResult results)
        {
            // In case of raw data output without headings
            if (results.Count == 1 && results[0].ContainsKey(string.Empty))
                FormatRaw(results);
            // Only single row result
            else if (results.Count == 1)
                FormatList(results);
            // List of results
            else
                FormatTable(results);
        }

        private static void FormatRaw(CommandResult results)
        {
            string rawOutput = results[0][string.Empty];
            Console.Write(rawOutput);
        }

        public static void FormatTable(CommandResult results)
        {
            // No results
            if (results.Count == 0)
                return;

            Dictionary<string, int> columns = CalcColumnWidths(results);

            // Print header
            int columnCount = columns.Count;
            int currentCol = 0;
            string separator = string.Empty;
            foreach (string column in columns.Keys)
            {
                currentCol++;

                string paddedSeparator = new string('-', column.Length) + new string(' ', columns[column] - column.Length + 1);
                string paddedValue = column.PadRight(columns[column]);

                // The most right column does not need to be padded
                if (columnCount == currentCol)
                {
                    paddedSeparator = new string('-', column.Length);
                    paddedValue = column;
                }
                separator += paddedSeparator;

                Console.Write("{0} ", paddedValue);
            }
            Console.WriteLine();
            Console.WriteLine(separator);

            // Print data
            foreach (ResultRecord row in results)
            {
                currentCol = 0;
                foreach (string column in columns.Keys)
                {
                    currentCol++;
                    string value = row[column.Trim()];

                    if (value == null)
                        value = string.Empty;

                    // The most right column does not need to be padded
                    string paddedValue = value.PadRight(columns[column] + 1);
                    if (currentCol == columnCount)
                        paddedValue = value;

                    Console.Write(paddedValue);
                }
                Console.WriteLine();
            }
        }

        public static void FormatList(CommandResult results)
        {
            // No results
            if (results.Count == 0)
                return;

            Dictionary<string, int> columns = CalcColumnWidths(results);

            // Determine maximum column width
            int maxColumnLength = -1;
            foreach (string column in columns.Keys)
            {
                if (column.Length > maxColumnLength)
                    maxColumnLength = column.Length;
            }

            // Print data
            foreach (ResultRecord result in results)
            {
                foreach (string column in columns.Keys)
                {
                    Console.WriteLine("{0} : {1}", column.PadRight(maxColumnLength), result[column]);
                }
                Console.WriteLine();
            }
        }

        private static Dictionary<string, int> CalcColumnWidths(CommandResult results)
        {
            Dictionary<string, int> columnWidths = new Dictionary<string, int>(results[0].Keys.Count);

            foreach (string key in results[0].Keys)
            {
                columnWidths.Add(key, key.Length);
            }

            string[] columnNames = new string[columnWidths.Count];
            columnWidths.Keys.CopyTo(columnNames, 0);

            // Iterate over results in output
            foreach (ResultRecord result in results)
            {
                // Iterate over columns of reach result
                foreach (string key in columnNames)
                {
                    string value = result[key];
                    if (value == null)
                        continue;

                    if (value.Length > columnWidths[key])
                        columnWidths[key] = value.Length;
                }
            }

            return columnWidths;
        }
    }
}
