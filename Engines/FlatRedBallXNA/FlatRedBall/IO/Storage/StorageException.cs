#nullable enable
using System;
using System.IO;

namespace FlatRedBall.IO;

/// <summary>
/// A storage operation failed for a reason outside the caller's control (quota exceeded, disk full,
/// permission denied, browser storage unavailable). The backend's original exception is
/// <see cref="Exception.InnerException"/>. Invalid keys throw <see cref="ArgumentException"/> instead.
/// </summary>
public class StorageException : IOException
{
    /// <summary>Creates the exception with a message and the backend failure that caused it.</summary>
    public StorageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
