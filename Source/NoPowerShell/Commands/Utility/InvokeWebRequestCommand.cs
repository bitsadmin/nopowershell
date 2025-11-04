using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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
            // Obtain generic parameters
            base.Execute(pipeIn);

            // Obtain Invoke-WebRequest specific parameters
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

            _results = MakeRequest(uri, method, skipCertificateCheck, proxy, proxyCredential, proxyUseDefaultCredentials, useragent, outfile, verbose).GetAwaiter().GetResult();

            return _results;
        }

        static async Task<CommandResult> MakeRequest(string uri, string method, bool skipCertificateCheck, string proxy, string proxyCredential, bool proxyUseDefaultCredentials, string useragent, string outfile, bool verbose)
        {
            CommandResult results = new CommandResult();
            HttpClientHandler handler = new HttpClientHandler();

            // Support displaying certificate chain if -Verbose flag is specified
            CommandResult certificateChain = new CommandResult();
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                if (verbose)
                {
                    foreach (X509ChainElement chainElement in chain.ChainElements)
                    {
                        certificateChain.Add(GetCertificateDetails(chainElement.Certificate));
                    }
                }

                if (skipCertificateCheck)
                    return true;

                return sslPolicyErrors == SslPolicyErrors.None;
            };

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

                HttpResponseMessage response;
                try
                {
                    // Download file
                    if (!string.IsNullOrEmpty(outfile))
                    {
                        // Send request
                        response = await client.SendAsync(request);

                        if (verbose)
                        {
                            Program.WriteVerbose(
                                "Received HTTP/{0} {1}-byte response of content type {2}",
                                response.Version,
                                response.Content.Headers.ContentLength,
                                response.Content.Headers.ContentType.MediaType
                            );
                        }

                        // Write the content to outfile
                        File.WriteAllBytes(outfile, response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult());
                    }
                    // Display to console if no outfile is specified
                    else
                    {
                        // Send request
                        response = await client.SendAsync(request);
                    }

                    // Complement with certificate details if in verbose mode
                    if (verbose)
                    {
                        certificateChain.Reverse();
                        Program.WriteVerbose("Certificate chain:\r\n{0}\r\n", ResultPrinter.FormatList(certificateChain).TrimEnd());
                    }

                    // Display HTTP response metadata
                    ResultRecord result = new ResultRecord()
                    {
                        { "StatusCode", ((int)response.StatusCode).ToString() },
                        { "StatusDescription", response.StatusCode.ToString() }
                    };

                    // Display HTTP response content or the filename in case -OutFile is specified
                    if (string.IsNullOrEmpty(outfile))
                    {
                        if (response.Content.Headers.ContentType.MediaType == "application/octet-stream")
                        {
                            result.Add("Content", "...");
                            result.Add("RawContentLength", response.Content.Headers.ContentLength.Value.ToString());
                        }
                        else
                        {
                            result.Add("Content", await response.Content.ReadAsStringAsync());
                        }
                    }
                    else
                        result.Add("File Name", outfile);

                    // Add headers
                    result.Add("Headers", response.Headers.ToString().Trim());

                    results.Add(result);
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

        private static ResultRecord GetCertificateDetails(X509Certificate2 certificate)
        {
            return new ResultRecord()
            {
                {"Subject", certificate.Subject },
                {"Issuer", certificate.Issuer },
                {"Valid From", certificate.NotBefore.ToFormattedString() },
                {"Valid Until", certificate.NotAfter.ToFormattedString() },
                {"Thumbprint", certificate.Thumbprint },
                {"Serial Number", certificate.SerialNumber },
                {"Signature Algorithm", certificate.SignatureAlgorithm.FriendlyName },
                //{"Public Key", Convert.ToBase64String(certificate.GetPublicKey()) }
            };
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
                    new StringArgument("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko"), // Internet Explorer on Windows 10 User Agent
                    new StringArgument("Method", "GET"),
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
                        "iwr ifconfig.io/ip -UserAgent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36 Edg/142.0.0.0\""
                    ),
                    new ExampleEntry("View external IP using explicit proxy", "Invoke-WebRequest https://ifconfig.io/ip -Proxy http://proxy:8080"),
                    new ExampleEntry("Download file from the Internet to disk", "wget https://live.sysinternals.com/psexec.exe -OutFile C:\\Tmp\\psexec.exe"),
                    new ExampleEntry("Perform request ignoring invalid TLS certificates", "iwr https://74.242.189.11/about_this_site.txt -SkipCertificateCheck"),
                    new ExampleEntry("Show certificate chain details", "Invoke-WebRequest -Verbose -SkipCertificateCheck https://74.242.189.11/about_this_site.txt")
                };
            }
        }
    }
}
