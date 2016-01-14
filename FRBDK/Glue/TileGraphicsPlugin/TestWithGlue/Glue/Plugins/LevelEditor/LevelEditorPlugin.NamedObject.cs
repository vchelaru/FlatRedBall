namespace PluginTestbed.LevelEditor
{
    public partial class LevelEditorPlugin
    {
        public void ReactToNamedObjectChangedValue(string changedMember, object oldValue)
        {
            if (changedMember == "CurrentState")
                _selectionInterface.RefreshVariables(true);
        }
    }
}
