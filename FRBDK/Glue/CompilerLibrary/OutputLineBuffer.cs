using System.Collections.Generic;

namespace CompilerLibrary
{
    /// <summary>
    /// Collects output lines from any thread so a view can drain them on a timer.
    /// </summary>
    /// <remarks>
    /// Exists so printing output never touches the UI thread at the call site. Callers include the live
    /// edit connection manager, which prints while holding its connection lock - marshalling from there
    /// with a blocking Invoke deadlocks the editor, and marshalling with a non-blocking post instead
    /// queues an unbounded number of dispatcher operations when output is chatty. Buffering avoids both.
    /// </remarks>
    public class OutputLineBuffer
    {
        readonly List<OutputLine> lines = new List<OutputLine>();

        public struct OutputLine
        {
            public string Text;
            public bool IsError;
        }

        public int Count
        {
            get
            {
                lock (lines)
                {
                    return lines.Count;
                }
            }
        }

        /// <summary>
        /// Splits <paramref name="text"/> into lines and queues the non-blank ones. Null or empty text
        /// queues nothing.
        /// </summary>
        public void Add(string text, bool isError)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var split = text.Split('\n');

            lock (lines)
            {
                foreach (var line in split)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(new OutputLine { Text = line, IsError = isError });
                    }
                }
            }
        }

        /// <summary>
        /// Returns everything queued and empties the buffer.
        /// </summary>
        public OutputLine[] TakeAll()
        {
            lock (lines)
            {
                var toReturn = lines.ToArray();
                lines.Clear();
                return toReturn;
            }
        }

        public void Clear()
        {
            lock (lines)
            {
                lines.Clear();
            }
        }
    }
}
