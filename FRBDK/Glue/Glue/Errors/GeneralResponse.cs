using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    public class GeneralResponse
    {
        public static GeneralResponse SuccessfulResponse => new GeneralResponse { Succeeded = true };

        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
