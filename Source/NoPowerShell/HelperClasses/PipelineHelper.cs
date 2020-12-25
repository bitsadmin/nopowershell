using System;
using System.Collections.Generic;
using System.Text;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.HelperClasses
{
    class PipelineHelper
    {
        public static string PipeToString(CommandResult pipeIn)
        {
            if (pipeIn == null)
                return string.Empty;

            List<string> lines = new List<string>(pipeIn.Count);
            foreach (ResultRecord r in pipeIn)
            {
                string[] arr = new string[r.Values.Count];
                r.Values.CopyTo(arr, 0);
                string outline = string.Join(" | ", arr);
                lines.Add(outline);
            }

            return string.Join(System.Environment.NewLine, lines.ToArray());
        }
    }
}
