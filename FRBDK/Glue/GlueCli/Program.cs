namespace GlueCli;

public static class Program
{
    // STA: plugin StartUp methods build real WPF controls, which throw on an MTA thread.
    [STAThread]
    public static int Main(string[] args)
    {
        // Glue and its plugins write to the console here and there. Only the result belongs on stdout, so
        // anything else goes to stderr and a --json caller can parse stdout as-is.
        var stdout = Console.Out;
        Console.SetOut(Console.Error);

        var exitCode = GlueCliRunner.Run(args, stdout, Console.Error, new InstalledPluginsHost());

        stdout.Flush();
        Console.Error.Flush();

        // Plugins can leave foreground threads and timers running, which would keep the process alive after
        // the result is printed.
        Environment.Exit(exitCode);
        return exitCode;
    }
}
