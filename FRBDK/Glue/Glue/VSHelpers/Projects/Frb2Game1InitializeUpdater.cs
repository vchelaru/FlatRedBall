using System.IO;
using System.Text.RegularExpressions;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// Rewrites a freshly created FRB2 game's Game1.cs to load Glue's project file instead of the
    /// template's hardcoded GameScreen. FRB2 projects generate no code (see <see cref="Frb2Project"/>),
    /// so this one line has to be written into hand-written code somewhere for the game to actually load
    /// Glue's Screens/Entities at runtime.
    /// </summary>
    /// <remarks>
    /// Only called the moment Glue creates a project's very first .gluj (see
    /// <c>ProjectLoader.LoadProject</c>) - at that point Game1.cs is still the untouched template, so
    /// this is the one safe point to write into an otherwise hands-off FRB2 project. The regex match
    /// against the exact default call is a second guard on top of that "just created" caller: if
    /// Game1.cs does not contain the default call verbatim, this leaves the file alone rather than
    /// guessing.
    /// </remarks>
    public static class Frb2Game1InitializeUpdater
    {
        static readonly Regex DefaultInitializeCall = new Regex(
            @"FlatRedBallService\.Default\.Initialize\s*<\s*GameScreen\s*>\s*\(\s*this\s*\)\s*;",
            RegexOptions.Compiled);

        /// <param name="game1FilePath">Full path to Game1.cs.</param>
        /// <param name="glueProjectContentRelativePath">
        /// The .gluj's path relative to the content root, e.g. "Content/FrbEditor/MyGame.Common.gluj".
        /// </param>
        /// <returns>True if Game1.cs was found and still had the default call, and was rewritten.</returns>
        public static bool TryUpdateGame1ToLoadGlueProject(string game1FilePath, string glueProjectContentRelativePath)
        {
            if (string.IsNullOrEmpty(game1FilePath) || !File.Exists(game1FilePath))
            {
                return false;
            }

            var contents = File.ReadAllText(game1FilePath);

            if (!DefaultInitializeCall.IsMatch(contents))
            {
                return false;
            }

            var updated = DefaultInitializeCall.Replace(contents,
                $"FlatRedBallService.Default.Initialize(this, \"{glueProjectContentRelativePath}\");");

            File.WriteAllText(game1FilePath, updated);

            return true;
        }
    }
}
