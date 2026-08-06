using System;
using System.IO;
using Shouldly;

namespace GlueUnitTests.TestSupport;

// Locating the checkout from the test assembly, for tests that read repo files off disk rather than
// going through Glue. Several older tests still carry their own copy of this walk; new ones should
// use this.
public static class RepoPaths
{
    private static string frbRoot;

    /// <summary>
    /// The FlatRedBall repo root -- the folder holding Templates and .github.
    /// </summary>
    public static string FrbRoot => frbRoot ??= FindFrbRoot();

    /// <summary>
    /// The folder holding both the FlatRedBall and Gum checkouts. The release tooling's paths are
    /// relative to this, not to either repo, because GumCore is built from Gum's source but
    /// published by FlatRedBall.
    /// </summary>
    public static string CheckoutRoot
    {
        get
        {
            var checkoutRoot = Directory.GetParent(FrbRoot)!.FullName;
            Directory.Exists(Path.Combine(checkoutRoot, "Gum")).ShouldBeTrue(
                $"Expected a sibling Gum checkout at {Path.Combine(checkoutRoot, "Gum")}.");
            return checkoutRoot;
        }
    }

    private static string FindFrbRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Templates")) &&
                File.Exists(Path.Combine(directory.FullName, ".github", "workflows", "Engine.yml")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the FlatRedBall repo root above " + AppContext.BaseDirectory);
    }
}
