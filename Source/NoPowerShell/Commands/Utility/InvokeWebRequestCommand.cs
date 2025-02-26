using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class InvokeWebRequest : PSCommand
    {
        public InvokeWebRequest(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            string uri = _arguments.Get<StringArgument>("URI").Value;
            string useragent = _arguments.Get<StringArgument>("UserAgent").Value;
            string outfile = _arguments.Get<StringArgument>("OutFile").Value;

            // Add http:// prefix if no protocol is present
            if (!uri.Contains("://"))
                uri = "http://" + uri;

            _results = MakeRequest(uri, useragent, outfile).GetAwaiter().GetResult();

            return _results;
        }

        static async Task<CommandResult> MakeRequest(string uri, string useragent, string outfile)
        {
            CommandResult results = new CommandResult();

            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient())
            {
                // Optionally, specify a user agent
                if (!string.IsNullOrEmpty(useragent))
                    client.DefaultRequestHeaders.Add("User-Agent", useragent);

                // Display to console if no outfile is specified
                if (!string.IsNullOrEmpty(outfile))
                {
                    // Send a GET request
                    HttpResponseMessage response = await client.GetAsync(uri);

                    results.Add(new ResultRecord()
                    {
                        { "StatusCode", ((int)response.StatusCode).ToString() },
                        { "StatusDescription", response.StatusCode.ToString() },
                        { "Content", await response.Content.ReadAsStringAsync() },
                        { "Headers", response.Headers.ToString() }
                    });
                }
                // Download file
                else
                {
                    // Send a GET request
                    HttpResponseMessage response = client.GetAsync(uri).GetAwaiter().GetResult();

                    Console.WriteLine(
                        "Received HTTP/{0} {1}-byte response of content type {2}",
                        response.Version,
                        response.Content.Headers.ContentLength,
                        response.Content.Headers.ContentType.MediaType);

                    // Write the content to outfile
                    File.WriteAllBytes(outfile, response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult());

                    Console.WriteLine("File Name: {0}", outfile);
                }
            }

            return results;
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
                    new StringArgument("OutFile", true),
                    new StringArgument("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko", true)
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Gets content from a web page on the Internet."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry(
                        "View external IP address using custom user agent",
                        "iwr ifconfig.io/ip -UserAgent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.3\""
                    ),
                    new ExampleEntry(
                        "Download file from the Internet",
                        new List<string>()
                        {
                            "Invoke-WebRequest http://myserver.me/nc.exe",
                            "wget http://myserver.me/nc.exe"
                        }
                    ),
                    new ExampleEntry("Download file from the Internet specifying the destination", "wget http://myserver.me/nc.exe -OutFile C:\\Tmp\\netcat.exe"),
                };
            }
        }
    }
}
