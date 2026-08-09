using FlatRedBall.Glue.Plugins;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.Managers
{
    /// <summary>
    /// Covers which assembly gets blamed for an unhandled exception. Blaming the wrong one is not
    /// cosmetic: Glue shuts the blamed plugin down and shows the user its name and file path, so a
    /// misattributed error both disables working functionality and sends bug reports to the wrong place.
    /// </summary>
    public class PluginExceptionAttributionTests
    {
        static Assembly ThisAssembly => typeof(PluginExceptionAttributionTests).Assembly;

        static Exception Thrown(Action action)
        {
            try
            {
                action();
                throw new InvalidOperationException("the action was supposed to throw");
            }
            catch (Exception e)
            {
                return e;
            }
        }

        [Fact]
        public void ExceptionThrownFromTheAssembly_IsAttributedToIt()
        {
            var exception = Thrown(() => throw new InvalidOperationException("from this test assembly"));

            PluginManager.WasExceptionThrownByAssembly(exception, ThisAssembly).ShouldBeTrue();
        }

        [Fact]
        public void ExceptionThrownFromElsewhere_IsNotAttributedToThisAssembly()
        {
            // Thrown entirely inside Newtonsoft, so no frame here belongs to the test assembly.
            var exception = Thrown(() => JsonConvert.DeserializeObject<int>("not json at all"));

            PluginManager.WasExceptionThrownByAssembly(exception, typeof(JsonConvert).Assembly).ShouldBeTrue();
        }

        /// <summary>
        /// The actual reported bug. A WPF exception has Source "PresentationCore", and attribution used to
        /// match any assembly that merely *referenced* Source. Every WPF plugin references PresentationCore,
        /// so an arbitrary innocent plugin was blamed and shut down.
        /// </summary>
        [Fact]
        public void AssemblyThatMerelyReferencesTheThrowingAssembly_IsNotBlamed()
        {
            var exception = Thrown(() => JsonConvert.DeserializeObject<int>("not json at all"));

            var throwingAssemblyName = typeof(JsonConvert).Assembly.GetName().Name;

            exception.Source.ShouldBe(throwingAssemblyName,
                "this test relies on Source being the throwing framework assembly");

            // Glue's own assembly references the throwing assembly but has no frames in this stack, which
            // is exactly the shape that got an innocent plugin blamed and shut down.
            var referencesButDidNotThrow = typeof(PluginManager).Assembly;
            referencesButDidNotThrow.GetReferencedAssemblies()
                .ShouldContain(x => x.Name == throwingAssemblyName,
                    "this test relies on the candidate assembly referencing the throwing assembly");

            PluginManager.WasExceptionThrownByAssembly(exception, referencesButDidNotThrow).ShouldBeFalse();
        }

        [Fact]
        public void ExceptionBuriedInAnInnerException_IsStillAttributed()
        {
            var inner = Thrown(() => throw new InvalidOperationException("from this test assembly"));
            var wrapped = new Exception("wrapped by something else", inner);

            PluginManager.WasExceptionThrownByAssembly(wrapped, ThisAssembly).ShouldBeTrue();
        }

        [Fact]
        public void ExceptionBuriedInAnAggregateException_IsStillAttributed()
        {
            var inner = Thrown(() => throw new InvalidOperationException("from this test assembly"));
            var aggregate = new AggregateException(new Exception("unrelated"), inner);

            PluginManager.WasExceptionThrownByAssembly(aggregate, ThisAssembly).ShouldBeTrue();
        }

        [Fact]
        public void ExceptionWithNoStackTrace_BlamesNobody()
        {
            // Never thrown, so it has no frames and no Source.
            var exception = new InvalidOperationException("constructed but not thrown");

            PluginManager.WasExceptionThrownByAssembly(exception, ThisAssembly).ShouldBeFalse();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void NullArguments_BlameNobody(bool nullException, bool nullAssembly)
        {
            var exception = nullException ? null : new Exception("some error");
            var assembly = nullAssembly ? null : ThisAssembly;

            PluginManager.WasExceptionThrownByAssembly(exception, assembly).ShouldBeFalse();
        }
    }
}
