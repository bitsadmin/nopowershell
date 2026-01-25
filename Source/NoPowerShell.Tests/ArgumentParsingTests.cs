using System;
using NoPowerShell.Arguments;
using NoPowerShell.HelperClasses;
using Xunit;

namespace NoPowerShell.Tests
{
    public class ArgumentParsingTests
    {
        private const string CommandName = "Test-Command";

        private static ArgumentList BuildSupportedArguments()
        {
            return new ArgumentList
            {
                new StringArgument("Name", false),
                new IntegerArgument("Count", 1),
                new BoolArgument("Force"),
            };
        }

        [Fact]
        public void Parses_Named_String_And_Int_Arguments()
        {
            // Arrange
            var args = new[] { "-Name", "Value1", "-Count", "42" };
            var parsedArguments = ArgumentParser.ParseArguments(args, BuildSupportedArguments(), CommandName);

            // Act
            var name = parsedArguments.Get<StringArgument>("Name")?.Value;
            var count = parsedArguments.Get<IntegerArgument>("Count")?.Value;

            // Assert
            Assert.Equal("Value1", name);
            Assert.Equal(42, count);
        }

        [Fact]
        public void Parses_Named_String_And_Int_Arguments_Without_Parameter_Names()
        {
            // Arrange
            var args = new[] { "Value1", "42" };
            var parsedArguments = ArgumentParser.ParseArguments(args, BuildSupportedArguments(), CommandName);

            // Act
            var name = parsedArguments.Get<StringArgument>("Name")?.Value;
            var count = parsedArguments.Get<IntegerArgument>("Count")?.Value;

            // Assert
            Assert.Equal("Value1", name);
            Assert.Equal(42, count);
        }

        [Fact]
        public void Parses_Bool_Switch()
        {
            var args = new[] { "-Force" };
            var parsedArguments = ArgumentParser.ParseArguments(args, BuildSupportedArguments(), CommandName);

            var force = parsedArguments.Get<BoolArgument>("Force")?.Value;

            Assert.True(force);
        }

        [Fact]
        public void Throws_On_Missing_Mandatory_Argument()
        {
            var args = Array.Empty<string>();

            var ex = Assert.Throws<NoPowerShellException>(() =>
                ArgumentParser.ParseArguments(args, BuildSupportedArguments(), CommandName));

            Assert.Contains("Mandatory parameter 'Name' is missing", ex.Message);
        }

        [Fact]
        public void Throws_On_Missing_Int_Value()
        {
            var args = new[] { "-Name", "X", "-Count" };

            var ex = Assert.Throws<ParameterBindingException>(() =>
                ArgumentParser.ParseArguments(args, BuildSupportedArguments(), CommandName));

            Assert.Contains("A value for parameter 'Count' is missing", ex.Message);
        }
    }
}
