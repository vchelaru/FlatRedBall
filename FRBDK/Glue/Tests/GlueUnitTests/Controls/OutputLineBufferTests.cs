using CompilerLibrary;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GlueUnitTests.Controls
{
    public class OutputLineBufferTests
    {
        [Fact]
        public void Add_SplitsOnNewlinesAndDropsBlankLines()
        {
            var buffer = new OutputLineBuffer();

            buffer.Add("first\n\nsecond\n   \nthird", isError: false);

            var lines = buffer.TakeAll();
            lines.Select(x => x.Text).ShouldBe(new[] { "first", "second", "third" });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Add_IgnoresEmptyText(string text)
        {
            var buffer = new OutputLineBuffer();

            buffer.Add(text, isError: false);

            buffer.Count.ShouldBe(0);
        }

        [Fact]
        public void Add_RecordsWhetherTheLineWasErrorOutput()
        {
            var buffer = new OutputLineBuffer();

            buffer.Add("normal", isError: false);
            buffer.Add("bad", isError: true);

            var lines = buffer.TakeAll();
            lines.Single(x => x.Text == "normal").IsError.ShouldBeFalse();
            lines.Single(x => x.Text == "bad").IsError.ShouldBeTrue();
        }

        [Fact]
        public void TakeAll_EmptiesTheBuffer()
        {
            var buffer = new OutputLineBuffer();
            buffer.Add("something", isError: false);

            buffer.TakeAll().Length.ShouldBe(1);

            buffer.Count.ShouldBe(0);
            buffer.TakeAll().ShouldBeEmpty();
        }

        [Fact]
        public void Clear_DiscardsQueuedLines()
        {
            var buffer = new OutputLineBuffer();
            buffer.Add("a\nb\nc", isError: false);

            buffer.Clear();

            buffer.Count.ShouldBe(0);
        }

        /// <summary>
        /// The whole point of the buffer is that any thread can print without touching the UI thread, so
        /// concurrent adds must not lose lines or corrupt the list.
        /// </summary>
        [Fact]
        public void Add_IsSafeFromManyThreadsAtOnce()
        {
            var buffer = new OutputLineBuffer();
            const int threadCount = 8;
            const int linesPerThread = 500;

            Parallel.For(0, threadCount, threadIndex =>
            {
                for (int i = 0; i < linesPerThread; i++)
                {
                    buffer.Add($"thread {threadIndex} line {i}", isError: false);
                }
            });

            buffer.TakeAll().Length.ShouldBe(threadCount * linesPerThread);
        }
    }
}
