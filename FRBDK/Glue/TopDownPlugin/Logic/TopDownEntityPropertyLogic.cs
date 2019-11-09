using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Logic
{
    public static class TopDownEntityPropertyLogic
    {
        public static bool GetIfIsTopDown(IElement element)
        {
            return element.Properties
                .GetValue<bool>(nameof(TopDownEntityViewModel.IsTopDown));
        }
    }
}
