using FlatRedBall.Glue.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.Errors
{
    public class DelegateBasedErrorViewModel : ErrorViewModel
    {
        public override string UniqueId => Details;

        public Func<bool> IfIsFixedDelegate;
        public override bool GetIfIsFixed() =>
            IfIsFixedDelegate?.Invoke() ?? false;

    }
}
