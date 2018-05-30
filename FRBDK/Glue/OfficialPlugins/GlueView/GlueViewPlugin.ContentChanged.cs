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
            // May 29, 2018 - this used to indiscriminately refresh GView on any file change.
            // That's a no-no, files change all the time and this shouldn't happen. I'm going
            // to comment it out for now and bring it back in with a check on the file name in
            // the future.

            var shouldRefreshGView = false;
            if(shouldRefreshGView)
            {
                _selectionInterface.RefreshGlueProject(false);

            }

        }

    }
}
