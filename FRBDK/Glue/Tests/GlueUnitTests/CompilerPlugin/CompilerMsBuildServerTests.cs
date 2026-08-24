using CompilerLibrary.ViewModels;
using CompilerPlugin.Managers;
using CompilerPlugin.Models;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CompilerPlugin
{
    /// <summary>
    /// Covers issue #2200: an opt-in "Use MSBuild Server" setting that sets
    /// DOTNET_CLI_USE_MSBUILD_SERVER on the environment of the restore/build processes Glue spawns,
    /// scoped to that child process only (CreateProcess never calls Start(), so this pins the
    /// ProcessStartInfo it builds without launching a real process).
    /// </summary>
    public class CompilerMsBuildServerTests
    {
        static Compiler CreateCompiler(bool? useMsBuildServer)
        {
            var compiler = new Compiler(CompilerViewModel.Self);
            if (useMsBuildServer.HasValue)
            {
                compiler.BuildSettingsUser = new BuildSettingsUser { UseMsBuildServer = useMsBuildServer.Value };
            }
            return compiler;
        }

        [Fact]
        public void CreateProcess_WhenUseMsBuildServerIsTrue_SetsEnvironmentVariable()
        {
            var compiler = CreateCompiler(useMsBuildServer: true);

            var process = compiler.CreateProcess("dotnet", "msbuild");

            process.StartInfo.EnvironmentVariables["DOTNET_CLI_USE_MSBUILD_SERVER"].ShouldBe("1");
        }

        [Fact]
        public void CreateProcess_WhenUseMsBuildServerIsFalse_DoesNotSetEnvironmentVariable()
        {
            var compiler = CreateCompiler(useMsBuildServer: false);

            var process = compiler.CreateProcess("dotnet", "msbuild");

            process.StartInfo.EnvironmentVariables.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER").ShouldBeFalse();
        }

        [Fact]
        public void CreateProcess_WhenBuildSettingsUserIsNull_DoesNotSetEnvironmentVariable()
        {
            var compiler = CreateCompiler(useMsBuildServer: null);

            var process = compiler.CreateProcess("dotnet", "msbuild");

            process.StartInfo.EnvironmentVariables.ContainsKey("DOTNET_CLI_USE_MSBUILD_SERVER").ShouldBeFalse();
        }

        [Fact]
        public void ShouldWarmUpMsBuildServer_ReflectsBuildSettingsUser()
        {
            CreateCompiler(useMsBuildServer: true).ShouldWarmUpMsBuildServer.ShouldBeTrue();
            CreateCompiler(useMsBuildServer: false).ShouldWarmUpMsBuildServer.ShouldBeFalse();
            CreateCompiler(useMsBuildServer: null).ShouldWarmUpMsBuildServer.ShouldBeFalse();
        }

        [Theory]
        [InlineData(false, true, true)]   // turning it on mid-session should warm up
        [InlineData(true, true, false)]   // already on, OK clicked again - don't refire
        [InlineData(true, false, false)]  // turning it off - nothing to warm up
        [InlineData(false, false, false)] // stayed off
        public void ShouldWarmUpOnSettingsChange_OnlyFiresOnOffToOnTransition(
            bool wasUsingMsBuildServer, bool isUsingMsBuildServerNow, bool expected)
        {
            Compiler.ShouldWarmUpOnSettingsChange(wasUsingMsBuildServer, isUsingMsBuildServerNow)
                .ShouldBe(expected);
        }
    }
}
