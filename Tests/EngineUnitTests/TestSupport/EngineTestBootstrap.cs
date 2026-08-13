namespace EngineUnitTests.TestSupport;

internal static class EngineTestBootstrap
{
    static bool s_initialized;

    // FlatRedBallServices initialization is process-wide static state and most of it (once set) can't
    // be re-run without crashing (e.g. InstructionManager.CreateInterpolators re-adds keys to a static
    // dictionary), so every test class that needs it must share this one guard rather than each keeping
    // its own. Callers must also be in EngineStatefulTestCollection (see below) - xunit runs separate
    // collections in parallel on separate threads, and whichever thread calls InitializeCommandLine
    // first becomes FlatRedBall's "primary thread"; a class landing on a different thread then fails
    // with "Objects can only be added on the primary thread" the moment it touches SpriteManager.
    public static void EnsureInitialized()
    {
        if (!s_initialized)
        {
            FlatRedBall.FlatRedBallServices.InitializeCommandLine();
            s_initialized = true;
        }
    }
}

[CollectionDefinition(nameof(EngineStatefulTestCollection), DisableParallelization = true)]
public class EngineStatefulTestCollection
{
}
