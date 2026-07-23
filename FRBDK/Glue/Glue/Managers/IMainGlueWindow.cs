using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Abstraction over Glue's main WinForms window (<c>global::Glue.MainGlueWindow</c>), reached from all
    /// over the codebase through the static <c>MainGlueWindow.Self</c>. The default value is the real
    /// WinForms window (unchanged - still only set once <c>Program.cs</c> constructs it), so production
    /// behavior is identical; test hosts can swap in a fake that doesn't require a live message loop.
    ///
    /// Covers every member any call site outside <c>MainGlueWindow.cs</c> itself touches through
    /// <c>MainGlueWindow.Self</c>, found by grepping for both <c>MainGlueWindow.Self.&lt;member&gt;</c> and
    /// bare <c>MainGlueWindow.Self</c> passed by value (the latter is how the <see cref="IWin32Window"/>,
    /// <see cref="Width"/>, and <see cref="Height"/> members were found - a member-only grep misses a value
    /// assigned to a local and used later, e.g. <c>var window = MainGlueWindow.Self; window.Width</c>).
    /// <see cref="IWin32Window"/> covers every <c>ShowDialog</c>/<c>Show</c>/<c>MessageBox.Show</c> call site
    /// that passes <c>MainGlueWindow.Self</c> as an owner window. A couple of call sites need the concrete
    /// <c>Form</c> or <c>System.ComponentModel.ISynchronizeInvoke</c> (a WinForms <c>Timer</c>'s
    /// <c>SynchronizingObject</c>, and <c>Form.Owner</c>) - those stayed as narrow casts at the call site
    /// rather than widening the interface further for one or two consumers each.
    /// </summary>
    public interface IMainGlueWindow : IWin32Window
    {
        void Invoke(Action action);
        T Invoke<T>(Func<T> func);
        Task Invoke(Func<Task> func);
        Task<T> Invoke<T>(Func<Task<T>> func);
        IAsyncResult BeginInvoke(Delegate method);

        // Fully qualified: unqualified "PropertyGrid" here would bind to the FlatRedBall.PropertyGrid
        // namespace (FlatRedBall.PropertyGrid/LateBinder.cs), which shadows the System.Windows.Forms type
        // because enclosing-namespace lookup (FlatRedBall -> FlatRedBall.PropertyGrid) wins over the
        // "using System.Windows.Forms;" directive above.
        System.Windows.Forms.PropertyGrid PropertyGrid { get; set; }
        bool HasErrorOccurred { get; set; }
        bool IsDisposed { get; }
        string Text { get; set; }
        IContainer Components { get; }
        int NumberOfStoredRecentFiles { get; set; }
        int Width { get; set; }
        int Height { get; set; }

        void Close();
        void SyncMenuStripWithTheme(System.Windows.Controls.UserControl control);
        void TryGenerateImplicitWindowStylesFor(Assembly assembly);
    }
}
