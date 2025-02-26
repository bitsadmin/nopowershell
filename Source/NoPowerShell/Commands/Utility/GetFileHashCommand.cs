using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

/*
Author: @bitsadmin
Website: https://github.com/bitsadmin
License: BSD 3-Clause
*/

namespace NoPowerShell.Commands.Utility
{
    public class GetFileHashCommand : PSCommand
    {
        public GetFileHashCommand(string[] userArguments) : base(userArguments, SupportedArguments)
        {
        }

        public override CommandResult Execute(CommandResult pipeIn)
        {
            // Obtain cmdlet parameters
            string path = _arguments.Get<StringArgument>("Path").Value;
            string[] algorithms = _arguments.Get<StringArgument>("Algorithm").Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> supportedAlgorithms = new List<string>() { "MD5", "SHA1", "SHA256", "SHA384", "SHA512", "RIPEMD160", "All", "Common" }; //"MACTripleDES", 
            bool wildcardPath = path.Contains("*") || path.Contains("?");

            // Set default algorithm if not specified
            if (algorithms.Length == 0)
            {
                if (wildcardPath)
                    algorithms = new string[] { "SHA256" };
                else
                    algorithms = new string[] { "Common" };
            }

            // Validate specified algorithm(s)
            List<string> enabledAlgorithms = new List<string>();
            foreach (string algorithm in algorithms)
            {
                string foundAlgorithm = supportedAlgorithms.Find(alg => alg.Equals(algorithm, StringComparison.InvariantCultureIgnoreCase));
                if (string.IsNullOrEmpty(foundAlgorithm))
                    throw new NoPowerShellException($"Value {algorithm} not in list of supported values: {string.Join(",", supportedAlgorithms)}");

                // Unofficial "All" value for -Algorithm
                if (algorithm.ToUpperInvariant() == "ALL")
                {
                    enabledAlgorithms = supportedAlgorithms.GetRange(0, supportedAlgorithms.Count - 2);
                    break;
                }
                else if (algorithm.ToUpperInvariant() == "COMMON")
                {
                    enabledAlgorithms = new List<string>() { "MD5", "SHA1", "SHA256" };
                    break;
                }
                else
                {
                    enabledAlgorithms.Add(foundAlgorithm);
                }
            }

            // Determine if wildcard is specified
            if (wildcardPath)
            {
                string directory = Path.GetDirectoryName(path);
                string searchPattern = Path.GetFileName(path);
                
                // Get all files in the directory
                string[] files = Directory.GetFiles(directory, searchPattern);
                
                // Process each file
                foreach (string file in files)
                {
                    _results.AddRange(GenerateHashes(file, enabledAlgorithms).GetAwaiter().GetResult());
                }
            }
            else
            {
                // Perform hash calculation on single file
                _results = GenerateHashes(path, enabledAlgorithms).GetAwaiter().GetResult();
            }

            // Always return the results so the output can be used by the next command in the pipeline
            return _results;
        }

        static async Task<CommandResult> GenerateHashes(string path, List<string> enabledAlgorithms)
        {
            CommandResult results = new CommandResult();

            // Activate relevant algorithms
            Dictionary<string, HashAlgorithm> algorithmsToRun = new Dictionary<string, HashAlgorithm>();

            if (enabledAlgorithms.Contains("SHA1"))
                algorithmsToRun.Add("SHA1", SHA1.Create());
            if (enabledAlgorithms.Contains("SHA256"))
                algorithmsToRun.Add("SHA256", SHA256.Create());
            if (enabledAlgorithms.Contains("SHA384"))
                algorithmsToRun.Add("SHA384", SHA384.Create());
            if (enabledAlgorithms.Contains("SHA512"))
                algorithmsToRun.Add("SHA512", SHA512.Create());
            // For some reason the result is incorrect, so it is disabled for now
            //if (enabledAlgorithms.Contains("MACTripleDES"))
            //    algorithmsToRun.Add("MACTripleDES", new MACTripleDES());
            if (enabledAlgorithms.Contains("MD5"))
                algorithmsToRun.Add("MD5", MD5.Create());
            if (enabledAlgorithms.Contains("RIPEMD160"))
                algorithmsToRun.Add("RIPEMD160", RIPEMD160.Create());

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // Initialization
                int bufferSize = 1024 * 1024; // 1MB buffer size
                byte[] buffer = new byte[bufferSize];
                int bytesRead;

                // Read the file in blocks, updating each hash algorithm
                while ((bytesRead = await fs.ReadAsync(buffer, 0, bufferSize)) > 0)
                {
                    var tasks = new List<Task>();

                    foreach (string key in algorithmsToRun.Keys)
                    {
                        HashAlgorithm algorithm = algorithmsToRun[key];
                        tasks.Add(Task.Run(() => algorithm.TransformBlock(buffer, 0, bytesRead, null, 0)));
                    }

                    await Task.WhenAll(tasks);
                }

                // Finalize hash computations
                foreach (string key in algorithmsToRun.Keys)
                {
                    HashAlgorithm algorithm = algorithmsToRun[key];
                    algorithm.TransformFinalBlock(buffer, 0, 0);
                    string hash = BitConverter.ToString(algorithm.Hash).Replace("-", "");
                    results.Add(
                        new ResultRecord()
                        {
                            { "Algorithm", key },
                            { "Hash",  hash },
                            { "Path", path }
                        }
                    );
                }
            }

            return results;
        }

        public static new CaseInsensitiveList Aliases
        {
            get { return new CaseInsensitiveList() { "Get-FileHash" }; }
        }

        public static new ArgumentList SupportedArguments
        {
            get
            {
                return new ArgumentList()
                {
                    new StringArgument("Path"),
                    new StringArgument("Algorithm", string.Empty, true),
                };
            }
        }

        public static new string Synopsis
        {
            get { return "Computes the hash value for a file by using a specified hash algorithm."; }
        }

        public static new ExampleEntries Examples
        {
            get
            {
                return new ExampleEntries()
                {
                    new ExampleEntry
                    (
                        "Calculate commonly used hashes (MD5,SHA1,SHA256) for file",
                        new List<string>()
                        {
                            "Get-FileHash C:\\Windows\\explorer.exe",
                            "Get-FileHash -Path C:\\Windows\\explorer.exe -Algorithm common"
                        }
                    ),
                    new ExampleEntry("Calculate SHA256 hash of a file", "Get-FileHash -Path C:\\Windows\\explorer.exe -Algorithm SHA256"),
                    new ExampleEntry("Calculate specific hashes for file", "Get-FileHash C:\\file.bin -Algorithm MD5,SHA1"),
                    new ExampleEntry("Calculate all supported hashes (MD5,SHA1,SHA256,SHA384,SHA512,RIPEMD160) for file", "Get-FileHash C:\\file.bin -Algorithm All")
                };
            }
        }
    }
}
