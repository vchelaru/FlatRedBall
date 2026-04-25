namespace AnimationEditor.Core.IO;

/// <summary>
/// Parses command-line arguments for the Animation Editor (IO12).
/// Separated into its own class so the logic can be unit-tested without
/// starting the Avalonia application.
/// </summary>
public static class CommandLineArgParser
{
    /// <summary>
    /// Returns the first argument that ends with <c>.achx</c> (case-insensitive),
    /// or <c>null</c> if no such argument is found.
    /// </summary>
    /// <param name="args">The application command-line arguments, as received in <c>Main(string[] args)</c>.</param>
    public static string? ParseFileArgument(string[]? args)
    {
        if (args is null || args.Length == 0)
            return null;

        return Array.Find(args, a =>
            !string.IsNullOrEmpty(a) &&
            a.EndsWith(".achx", StringComparison.OrdinalIgnoreCase));
    }
}
