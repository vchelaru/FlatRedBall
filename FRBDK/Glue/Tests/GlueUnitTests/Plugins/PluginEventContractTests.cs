using GlueUnitTests.TestSupport;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace GlueUnitTests.Plugins
{
    /// <summary>
    /// Plugins talk to each other by raising named events. The fire-and-forget flavor
    /// (<c>ReactToPluginEvent</c>) is harmless when nobody handles it, but the awaiting flavor
    /// (<c>ReactToPluginEventWithReturn</c>) polls <c>_pendingRequests</c> until an answer arrives and
    /// there is no timeout - see <c>PluginBase.ReactToPluginEventWithReturn</c>. An event name with no
    /// handler therefore does not fail, it hangs the awaiting code forever.
    /// </summary>
    public class PluginEventContractTests
    {
        static readonly Regex RaisedWaitingForReply = new Regex(
            @"(ReactToPluginEventWithReturn|_eventCallerWith(Result|Action|Return))\(\s*""(?<name>[A-Za-z0-9_]+)""");

        static readonly Regex Handled = new Regex(@"case\s+""(?<name>[A-Za-z0-9_]+)""\s*:");

        [Fact]
        public void EveryEventRaisedWaitingForAReply_HasAHandler()
        {
            var glueRoot = Path.Combine(RepoPaths.FrbRoot, "FRBDK", "Glue");

            var raised = new SortedDictionary<string, string>();
            var handled = new HashSet<string>();

            foreach (var file in EnumerateSourceFiles(glueRoot))
            {
                var text = File.ReadAllText(file);
                var isHandler = text.Contains("HandleEvent");

                foreach (var line in text.Split('\n'))
                {
                    if (line.TrimStart().StartsWith("//"))
                    {
                        continue;
                    }

                    foreach (Match match in RaisedWaitingForReply.Matches(line))
                    {
                        raised[match.Groups["name"].Value] = Path.GetFileName(file);
                    }

                    if (isHandler)
                    {
                        foreach (Match match in Handled.Matches(line))
                        {
                            handled.Add(match.Groups["name"].Value);
                        }
                    }
                }
            }

            raised.ShouldNotBeEmpty("the scan found nothing, so it is no longer matching how events are raised");

            var unhandled = raised.Where(pair => !handled.Contains(pair.Key)).ToList();

            unhandled.ShouldBeEmpty(
                "these events are awaited but nothing answers them, so whatever raises them hangs forever: " +
                string.Join(", ", unhandled.Select(pair => $"{pair.Key} ({pair.Value})")));
        }

        static IEnumerable<string> EnumerateSourceFiles(string root) =>
            Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(@"\bin\") && !file.Contains(@"\obj\"));
    }
}
