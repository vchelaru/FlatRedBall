namespace OfficialPlugins.GlueView
{
    public partial class GlueViewPlugin
    {
        public void ReactToNamedObjectChangedValue(string changedMember, object oldValue)
        {
            if (changedMember == "CurrentState")
                _selectionInterface.RefreshVariables(true);
        }
    }
}
