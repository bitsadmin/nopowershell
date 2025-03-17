using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            string method = _arguments.Get<StringArgument>("Method").Value;
            string proxy = _arguments.Get<StringArgument>("Proxy").Value;
            string proxyCredential = _arguments.Get<StringArgument>("ProxyCredential").Value;
            bool proxyUseDefaultCredentials = _arguments.Get<BoolArgument>("ProxyUseDefaultCredentials").Value;
            bool skipCertificateCheck = _arguments.Get<BoolArgument>("SkipCertificateCheck").Value;

            // Add http:// prefix if no protocol is present
            if (!uri.Contains("://"))
                uri = "http://" + uri;

            _results = MakeRequest(uri, method, skipCertificateCheck, proxy, proxyCredential, proxyUseDefaultCredentials, useragent, outfile).GetAwaiter().GetResult();

            return _results;
        }

        static async Task<CommandResult> MakeRequest(string uri, string method, bool skipCertificateCheck, string proxy, string proxyCredential, bool proxyUseDefaultCredentials, string useragent, string outfile)
        {
            CommandResult results = new CommandResult();

            HttpClientHandler handler = new HttpClientHandler();

            // Skip certificate check
            if (skipCertificateCheck)
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            // Proxy is specified
            if (!string.IsNullOrEmpty(proxy))
            {
                WebProxy webProxy = new WebProxy(proxy);

                // Proxy credentials are specified
                if (!string.IsNullOrEmpty(proxyCredential))
                {
                    string[] creds = proxyCredential.Split(new char[] { ':' });
                    if (creds.Length != 2)
                        throw new NoPowerShellException("Please specific ProxyCredential with username:password");

                    webProxy.Credentials = new NetworkCredential(creds[0], creds[1]);
                }

                // Configure use of default credentials
                webProxy.UseDefaultCredentials = proxyUseDefaultCredentials;

                // Enable proxy for request
                handler.Proxy = webProxy;
                handler.UseProxy = true;
            }

            // Create an instance of HttpClient
            using (HttpClient client = new HttpClient(handler))
            {
                // Use alternative HTTP method
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), uri);

                // Optionally, specify a user agent
                if (!string.IsNullOrEmpty(useragent))
                    client.DefaultRequestHeaders.Add("User-Agent", useragent);

                try
                {
                    // Download file
                    if (!string.IsNullOrEmpty(outfile))
                    {
                        // Send request
                        HttpResponseMessage response = await client.SendAsync(request);

                        Console.WriteLine(
                            "Received HTTP/{0} {1}-byte response of content type {2}",
                            response.Version,
                            response.Content.Headers.ContentLength,
                            response.Content.Headers.ContentType.MediaType
                        );

                        // Write the content to outfile
                        File.WriteAllBytes(outfile, response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult());

                        Console.WriteLine("File Name: {0}", outfile);
                    }
                    // Display to console if no outfile is specified
                    else
                    {
                        // Send request
                        HttpResponseMessage response = await client.SendAsync(request);

                        results.Add(new ResultRecord()
                        {
                            { "StatusCode", ((int)response.StatusCode).ToString() },
                            { "StatusDescription", response.StatusCode.ToString() },
                            { "Content", await response.Content.ReadAsStringAsync() },
                            { "Headers", response.Headers.ToString() }
                        });
                    }
                }
                catch (HttpRequestException e)
                {
                    if (e.InnerException != null)
                        throw new NoPowerShellException(e.InnerException.Message);
                    else
                        throw new NoPowerShellException(e.Message);
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
                    new StringArgument("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko", true), // Internet Explorer on Windows 10 User Agent
                    new StringArgument("Method", "GET", true),
                    new StringArgument("Proxy", true),
                    new StringArgument("ProxyCredential", true),
                    new BoolArgument("ProxyUseDefaultCredentials"),
                    new BoolArgument("SkipCertificateCheck")
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
                    new ExampleEntry("View external IP using explicit proxy", "Invoke-WebRequest https://ifconfig.io/ip -Proxy http://proxy:8080"),
                    new ExampleEntry(
                        "Download file from the Internet",
                        new List<string>()
                        {
                            "Invoke-WebRequest http://myserver.me/nc.exe",
                            "wget http://myserver.me/nc.exe"
                        }
                    ),
                    new ExampleEntry("Download file from the Internet specifying the destination", "wget http://myserver.me/nc.exe -OutFile C:\\Tmp\\netcat.exe"),
                    new ExampleEntry("Perform request ignoring invalid TLS certificates", "iwr https://1.2.3.4/file.txt -SkipCertificateCheck")
                };
            }
        }
    }
}
