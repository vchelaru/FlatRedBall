using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Test-only <see cref="IMainGlueWindow"/> that doesn't require a live WinForms message loop. Wired in by
/// <see cref="GlueTestBootstrap"/> so any production code path that reads <c>MainGlueWindow.Self</c> - e.g.
/// <c>PropertyGrid.Refresh()</c>, <c>HasErrorOccurred</c>, <c>Invoke</c>/<c>BeginInvoke</c> - gets a harmless
/// object instead of NRE-ing (Self is null in a plain xunit host, since only Program.cs ever constructs the
/// real WinForms window).
///
/// <c>Invoke</c>/<c>BeginInvoke</c> just run the delegate synchronously on the calling thread - fine for
/// tests, which are always single-threaded here. <see cref="PropertyGrid"/> is a real (not mocked)
/// <see cref="System.Windows.Forms.PropertyGrid"/> instance: constructing one doesn't require a running
/// message loop, and a real control means <c>PropertyGrid.Refresh()</c>/<c>SelectedObject</c> writes behave
/// exactly like production instead of needing a special test-only no-op path.
/// </summary>
internal class FakeMainGlueWindow : IMainGlueWindow
{
    public System.Windows.Forms.PropertyGrid PropertyGrid { get; set; } = new();
    public bool HasErrorOccurred { get; set; }
    public bool IsDisposed { get; private set; }
    public string Text { get; set; } = "";
    public IContainer Components { get; } = new Container();
    public int NumberOfStoredRecentFiles { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public IntPtr Handle { get; } = IntPtr.Zero;

    public void Invoke(Action action) => action();
    public T Invoke<T>(Func<T> func) => func();
    public Task Invoke(Func<Task> func) => func();
    public Task<T> Invoke<T>(Func<Task<T>> func) => func();
    // No call site inspects the returned IAsyncResult - null is fine after running synchronously.
    public IAsyncResult BeginInvoke(Delegate method) { method.DynamicInvoke(); return null!; }

    public void Close() => IsDisposed = true;
    public void SyncMenuStripWithTheme(System.Windows.Controls.UserControl control) { }
    public void TryGenerateImplicitWindowStylesFor(Assembly assembly) { }
}
