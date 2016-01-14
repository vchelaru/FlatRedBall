using System.ComponentModel.Composition;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.LevelEditor
{
    [Export(typeof(IGluxLoad))]
    public partial class LevelEditorPlugin : IGluxLoad
    {
        public void ReactToGluxLoad(FlatRedBall.Glue.SaveClasses.GlueProjectSave newGlux, string fileName)
        {
            _selectionInterface.SetGlueProjectFile(fileName, false);
        }

        public void ReactToGluxSave()
        {
            RefreshGlux();

            if (EditorLogic.CurrentStateSave != null)
            {
                _selectionInterface.SetState(EditorLogicSnapshot.CurrentState.Name, false);
            }
        }


        public void ReactToGluxUnload(bool isExiting)
        {
            _selectionInterface.UnloadProject(!isExiting);

        }


        public void RefreshGlux()
        {
            if (!_interactiveInterface.IgnoreNextRefresh)
            {
                _selectionInterface.RefreshGlueProject(false);
            }

            _interactiveInterface.IgnoreNextRefresh = false;
        }
    }
}
