using NoPowerShell.Commands.Management;
using NoPowerShell.HelperClasses;
using Xunit;

namespace NoPowerShell.Tests
{
    public class GetServiceCommandTests
    {
        [Fact]
        public void Execute_Throws_When_ScManager_Is_Combined_With_DisplayName()
        {
            var command = new GetServiceCommand(new[] { "-Name", "scmanager", "-DisplayName", "Service Control Manager" });

            var ex = Assert.Throws<NoPowerShellException>(() => command.Execute(null));

            Assert.Contains("do not specify -DisplayName", ex.Message);
        }
    }
}
