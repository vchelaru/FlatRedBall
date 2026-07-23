using System;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Abstraction over marshalling work onto Glue's UI thread. The default implementation
    /// (<see cref="WinFormsUiThreadMarshaller"/>) wraps <c>MainGlueWindow.Self.Invoke</c>/<c>BeginInvoke</c>,
    /// which requires a live <c>Form</c> with a running WinForms message loop. Test hosts can supply an
    /// inline implementation that just runs the delegate on the calling thread, so code that goes through
    /// <see cref="TaskManager.UiThreadMarshaller"/> (directly, or via <c>TaskManager.OnUiThread</c>/
    /// <c>BeginOnUiThread</c> or a <c>GlueTask</c> with <c>DoOnUiThread = true</c>) doesn't require a real window.
    /// </summary>
    public interface IUiThreadMarshaller
    {
        void Invoke(Action action);
        T Invoke<T>(Func<T> func);
        Task Invoke(Func<Task> func);
        Task<T> Invoke<T>(Func<Task<T>> func);
        void BeginInvoke(Action action);
    }
}
