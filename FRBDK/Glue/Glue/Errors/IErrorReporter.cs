using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    /// <summary>
    /// Interface for implementing an error reporter.
    /// </summary>
    /// <example>
    /// Each plugin Glue can include its own IErrorReporter implementation.
    /// The IErrorReporter implementation is responsible for returning all errors
    /// for the project. Once an individual error is returned through the ErrorReporter,
    /// it can resolve itself by overriding properties and methods provided by ErrorViewModel.
    /// 
    /// Example code:
    /// 
    /// public MyErrorReporter : IErrorReporter
    /// {
    ///   public ErrorViewModel[] GetAllErrors()
    ///   {
    ///     var errors = new List<ErrorViewModel>();
    ///     
    ///     // inspect whatever is relevant for this particular reporter and add to the errors list
    /// 
    ///     return errors.ToArray();
    ///   } 
    /// }
    /// </example>
    /// 
    public interface IErrorReporter
    {
        ErrorViewModel[] GetAllErrors();
    }
}
