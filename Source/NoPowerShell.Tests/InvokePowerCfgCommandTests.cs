using NoPowerShell.Arguments;
using NoPowerShell.Commands.Additional;
using NoPowerShell.HelperClasses;
using Xunit;

namespace NoPowerShell.Tests
{
    public class InvokePowerCfgCommandTests
    {
        [Fact]
        public void Aliases_Contain_Full_Command_And_PowerCfg()
        {
            var aliases = InvokePowerCfgCommand.Aliases;

            Assert.Equal("Invoke-PowerCfg", aliases[0]);
            Assert.Contains("powercfg", aliases);
        }

        [Fact]
        public void Constructor_Parses_SetActive_And_Timeout_Parameters()
        {
            var command = new InvokePowerCfgCommand(new[]
            {
                "-SetActive", "scheme_current",
                "-SetHibernateTimeoutAc", "0",
                "-SetStandbyTimeoutDc", "15"
            });

            StringArgument setActive = command.ParsedArguments.Get<StringArgument>("SetActive");
            IntegerArgument hibernateAc = command.ParsedArguments.Get<IntegerArgument>("SetHibernateTimeoutAc");
            IntegerArgument standbyDc = command.ParsedArguments.Get<IntegerArgument>("SetStandbyTimeoutDc");

            Assert.True(setActive.IsSet);
            Assert.Equal("scheme_current", setActive.Value);
            Assert.True(hibernateAc.IsSet);
            Assert.Equal(0, hibernateAc.Value);
            Assert.True(standbyDc.IsSet);
            Assert.Equal(15, standbyDc.Value);
        }

        [Fact]
        public void Constructor_Parses_List_Switch()
        {
            var command = new InvokePowerCfgCommand(new[] { "-List" });

            BoolArgument list = command.ParsedArguments.Get<BoolArgument>("List");

            Assert.True(list.Value);
        }

        [Fact]
        public void Constructor_Parses_Query_Switch_And_Scope_Arguments()
        {
            var command = new InvokePowerCfgCommand(new[]
            {
                "-Query",
                "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                "238c9fa8-0aad-41ed-83f4-97be242c8f20"
            });

            BoolArgument query = command.ParsedArguments.Get<BoolArgument>("Query");
            StringArgument querySchemeGuid = command.ParsedArguments.Get<StringArgument>("QuerySchemeGuid");
            StringArgument querySubgroupGuid = command.ParsedArguments.Get<StringArgument>("QuerySubgroupGuid");

            Assert.True(query.Value);
            Assert.True(querySchemeGuid.IsSet);
            Assert.Equal("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", querySchemeGuid.Value);
            Assert.True(querySubgroupGuid.IsSet);
            Assert.Equal("238c9fa8-0aad-41ed-83f4-97be242c8f20", querySubgroupGuid.Value);
        }

        [Fact]
        public void Execute_Throws_When_No_Action_Parameter_Is_Provided()
        {
            var command = new InvokePowerCfgCommand(new string[0]);

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("Specify at least one action parameter", ex.Message);
        }

        [Fact]
        public void Execute_Throws_For_Mutually_Exclusive_Lid_Parameters()
        {
            var command = new InvokePowerCfgCommand(new[] { "-DisableLidSleep", "-EnableLidSleep" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("cannot be used together", ex.Message);
        }

        [Fact]
        public void Execute_Throws_For_Negative_Hibernate_Timeout()
        {
            var command = new InvokePowerCfgCommand(new[] { "-SetHibernateTimeoutAc", "-1" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("SetHibernateTimeoutAc must be greater than or equal to 0", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_Query_Guid_Is_Provided_Without_Query_Switch()
        {
            var command = new InvokePowerCfgCommand(new[] { "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("QuerySchemeGuid and QuerySubgroupGuid require -Query", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_QuerySubgroupGuid_Is_Provided_Without_QuerySchemeGuid()
        {
            var command = new InvokePowerCfgCommand(new[] { "-Query", "-QuerySubgroupGuid", "238c9fa8-0aad-41ed-83f4-97be242c8f20" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("QuerySubgroupGuid cannot be provided without QuerySchemeGuid", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_Query_Is_Combined_With_Other_Actions()
        {
            var command = new InvokePowerCfgCommand(new[] { "-Query", "-List" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("Query cannot be combined with other action parameters", ex.Message);
        }

        [Fact]
        public void Execute_Throws_When_QuerySchemeGuid_Is_Invalid()
        {
            var command = new InvokePowerCfgCommand(new[] { "-Query", "not-a-guid" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("QuerySchemeGuid must be a valid GUID", ex.Message);
        }
    }
}
