using System.Collections.Generic;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Managers;

/// <summary>
/// While held, saving the .gluj, element files and .csproj does nothing, and generating code only records the
/// request. GlueCli's batch holds writes for all of its commands, then generates and saves once at the end, or
/// writes nothing when a command failed. The editor never holds.
/// </summary>
public static class ProjectWriteHold
{
    /// <summary>The code generation asked for while writes were held.</summary>
    public sealed record Requests(IReadOnlyCollection<GlueElement> Elements, bool GlobalContent, bool AllCode);

    static readonly object lockObject = new();
    static HashSet<GlueElement> elementCodeRequests;
    static bool globalContentCodeRequested;
    static bool allCodeRequested;

    public static bool IsHeld
    {
        get { lock (lockObject) { return elementCodeRequests != null; } }
    }

    public static void Hold()
    {
        lock (lockObject)
        {
            elementCodeRequests = new HashSet<GlueElement>();
            globalContentCodeRequested = false;
            allCodeRequested = false;
        }
    }

    /// <summary>Ends the hold and returns the code generation requested during it.</summary>
    public static Requests Release()
    {
        lock (lockObject)
        {
            var requests = new Requests((IReadOnlyCollection<GlueElement>)elementCodeRequests ?? new HashSet<GlueElement>(),
                globalContentCodeRequested, allCodeRequested);
            elementCodeRequests = null;
            globalContentCodeRequested = false;
            allCodeRequested = false;
            return requests;
        }
    }

    /// <summary>True, recording the request, when held: the caller then skips generating.</summary>
    internal static bool HoldElementCode(GlueElement element)
    {
        lock (lockObject)
        {
            elementCodeRequests?.Add(element);
            return elementCodeRequests != null;
        }
    }

    /// <summary>True, recording the request, when held: the caller then skips generating.</summary>
    internal static bool HoldGlobalContentCode()
    {
        lock (lockObject)
        {
            globalContentCodeRequested |= elementCodeRequests != null;
            return elementCodeRequests != null;
        }
    }

    /// <summary>True, recording the request, when held: the caller then skips generating.</summary>
    internal static bool HoldAllCode()
    {
        lock (lockObject)
        {
            allCodeRequested |= elementCodeRequests != null;
            return elementCodeRequests != null;
        }
    }
}
