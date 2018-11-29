using System;
using System.IO;
using System.Net;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;

/*
Author: @_bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands
{
    public class InvokeWebRequest : PSCommand
    {
        public InvokeWebRequest(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string uri = _arguments.Get<StringArgument>("URI").Value;
            string outfile = _arguments.Get<StringArgument>("OutFile").Value;

            // Try to automatically determine filename
            if (string.IsNullOrEmpty(outfile))
            {
                Uri href = new Uri(uri);
                outfile = Path.GetFileName(href.LocalPath);
            }

            // If still empty, use "out" as filename
            if (string.IsNullOrEmpty(outfile))
                outfile = "out";

            // Known issues:
            // - TLS 1.1+ is not supported by .NET Framework 2, so any site enforcing it will result in a connection error
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(uri, outfile);
            }

            return _results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Invoke-WebRequest", "curl", "iwr", "wget" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("URI"),
                    new StringArgument("OutFile", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets content from a web page on the Internet."; }
        }
    }
}
