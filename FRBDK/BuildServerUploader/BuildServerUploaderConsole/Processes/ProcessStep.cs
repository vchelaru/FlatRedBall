using System.Threading.Tasks;

namespace BuildServerUploaderConsole.Processes
{
    public class ProcessStep
    {
        #region Fields

        readonly string _message;
        private readonly IResults _results;

        #endregion

        #region Properties

        public string Message
        {
            get { return _message; }
        }

        public IResults Results
        {
            get { return _results; }
        }

        #endregion

        #region Methods

        public ProcessStep(string message, IResults results)
        {
            _message = message;
            _results = results;
        }

        public virtual void ExecuteStep() { }
        public virtual Task ExecuteStepAsync() { return Task.CompletedTask; }
        #endregion
    }
}
