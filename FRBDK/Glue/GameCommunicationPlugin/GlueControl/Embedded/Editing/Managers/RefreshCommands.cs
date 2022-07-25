using GlueControl.Dtos;
using GlueControl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Managers
{
    public enum TreeNodeRefreshType
    {
        All,
        NamedObjects,
        CustomVariables
        // eventually add more here as needed
    }

    class RefreshCommands : GlueCommandsStateBase
    {
        // I don't know if we care about the Task here:
        public async void RefreshTreeNodeFor(GlueElement element, TreeNodeRefreshType treeNodeRefreshType = TreeNodeRefreshType.All)
        {
            await base.SendMethodCallToGame(
                new RefreshCommandDto(),
                nameof(RefreshTreeNodeFor),
                GlueElementReference.From(element),
                TypedParameter.FromValue(treeNodeRefreshType));
        }
    }
}
