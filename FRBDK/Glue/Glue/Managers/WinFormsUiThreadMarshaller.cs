using System;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Managers
{
    /// <summary>
    /// Default production implementation of <see cref="IUiThreadMarshaller"/> - forwards straight to
    /// <c>global::Glue.MainGlueWindow.Self</c>'s real WinForms Invoke/BeginInvoke overloads. Zero behavior
    /// change from the code this was extracted from.
    /// </summary>
    public class WinFormsUiThreadMarshaller : IUiThreadMarshaller
    {
        public void Invoke(Action action) => global::Glue.MainGlueWindow.Self.Invoke(action);

        public T Invoke<T>(Func<T> func) => global::Glue.MainGlueWindow.Self.Invoke(func);

        public Task Invoke(Func<Task> func) => global::Glue.MainGlueWindow.Self.Invoke(func);

        public Task<T> Invoke<T>(Func<Task<T>> func) => global::Glue.MainGlueWindow.Self.Invoke(func);

        public void BeginInvoke(Action action) => global::Glue.MainGlueWindow.Self.BeginInvoke(action);
    }
}
