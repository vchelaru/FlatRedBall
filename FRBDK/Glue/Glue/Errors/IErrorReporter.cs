using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    public interface IErrorReporter
    {
        ErrorViewModel[] GetAllErrors();
    }
}
