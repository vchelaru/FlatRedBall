using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;

namespace OfficialPlugins.GlueView
{
    [Export(typeof(IContentFileChange))]
    public partial class GlueViewPlugin : IContentFileChange
    {
        public void ReactToFileChange(string fileName)
        {
            _selectionInterface.RefreshFile(false, fileName);
            _selectionInterface.RefreshGlueProject(false);
        }

    }
}
