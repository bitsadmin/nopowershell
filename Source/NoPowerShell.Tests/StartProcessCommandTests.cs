using NoPowerShell.Arguments;
using NoPowerShell.Commands.Management;
using NoPowerShell.HelperClasses;
using System;
using System.IO;
using Xunit;

namespace NoPowerShell.Tests
{
    public class StartProcessCommandTests
    {
        [Fact]
        public void Aliases_Contain_Full_Command_And_Start()
        {
            var aliases = StartProcessCommand.Aliases;

            Assert.Equal("Start-Process", aliases[0]);
            Assert.Contains("start", aliases);
        }

        [Fact]
        public void Constructor_Parses_Positional_FilePath_And_Arguments()
        {
            var command = new StartProcessCommand(new[] { "C:\\test.exe", "Arg1 Arg2" });

            StringArgument filePath = command.ParsedArguments.Get<StringArgument>("FilePath");
            StringArgument arguments = command.ParsedArguments.Get<StringArgument>("Arguments");
            StringArgument windowStyle = command.ParsedArguments.Get<StringArgument>("WindowStyle");

            Assert.True(filePath.IsSet);
            Assert.Equal("C:\\test.exe", filePath.Value);
            Assert.True(arguments.IsSet);
            Assert.Equal("Arg1 Arg2", arguments.Value);
            Assert.Equal("Normal", windowStyle.Value);
        }

        [Fact]
        public void Execute_Throws_When_WindowStyle_Is_Invalid()
        {
            string cmdPath = GetCmdPath();
            var command = new StartProcessCommand(new[] { cmdPath, "/c exit 0", "-WindowStyle", "InvalidValue" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("WindowStyle must be one of", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_RunasUser_Does_Not_Include_Credentials()
        {
            string cmdPath = GetCmdPath();
            var command = new StartProcessCommand(new[] { cmdPath, "/c exit 0", "-Verb", "runasuser" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("requires -Username and -Password", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_Only_One_Credential_Is_Provided()
        {
            string cmdPath = GetCmdPath();
            var command = new StartProcessCommand(new[] { cmdPath, "/c exit 0", "-Verb", "runasuser", "-Username", "user" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("must be specified together", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_Credentials_Are_Provided_Without_RunasUser()
        {
            string cmdPath = GetCmdPath();
            var command = new StartProcessCommand(new[]
            {
                cmdPath,
                "/c exit 0",
                "-Username", "user",
                "-Password", "pass"
            });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("can only be used together with -Verb runasuser", ex.Message);
        }

        [Fact]
        public void Execute_Starts_Process_And_Returns_Basic_Process_Info()
        {
            string cmdPath = GetCmdPath();
            var command = new StartProcessCommand(new[] { cmdPath, "/c exit 0", "-WindowStyle", "Hidden" });

            CommandResult result = command.Execute(null);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].ContainsKey("Id"));
            Assert.True(result[0].ContainsKey("ProcessName"));
            Assert.True(result[0].ContainsKey("FilePath"));
            Assert.Equal(cmdPath, result[0]["FilePath"]);
            Assert.False(string.IsNullOrEmpty(result[0]["Id"]));
            Assert.False(string.IsNullOrEmpty(result[0]["ProcessName"]));
        }

        private static string GetCmdPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        }
    }
}
