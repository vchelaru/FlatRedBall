using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace OfficialPlugins.GlueView
{
    [Export(typeof(IGluxLoad))]
    public partial class GlueViewPlugin : IGluxLoad
    {
        public void ReactToGluxLoad(FlatRedBall.Glue.SaveClasses.GlueProjectSave newGlux, string fileName)
        {
            _selectionInterface.SetGlueProjectFile(fileName, false);
        }

        public void ReactToGluxSave()
        {

            _selectionInterface.RefreshGlueProject(false);

            //if (EditorLogic.CurrentStateSave != null)
            //{
            //    _remoting.SetState(EditorLogicSnapshot.CurrentState.Name, false);
            //}
        }

        public void ReactToGluxUnload(bool isExiting)
        {
            _selectionInterface.UnloadProject(!isExiting);

        }

        public void RefreshGlux()
        {
            _selectionInterface.RefreshGlueProject(false);
        }
    }
}
