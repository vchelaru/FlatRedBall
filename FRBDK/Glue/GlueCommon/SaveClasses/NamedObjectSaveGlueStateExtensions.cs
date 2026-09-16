namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Moved from Glue.csproj's NamedObjectSaveExtensionMethodsGlue (#2276) - only needed
    /// IGlueStateCore widened with CurrentEntitySave/CurrentScreenSave, since GetMemberMembershipInfo/
    /// HasMemberWithName on EntitySave/ScreenSave themselves were already zero-coupling.
    /// </summary>
    public static class NamedObjectSaveGlueStateExtensions
    {
        public static MembershipInfo GetMemberMembershipInfo(string memberName)
        {
            var glueState = GlueStateCore.Self;

            if (glueState.CurrentScreenSave != null)
            {
                if (glueState.CurrentScreenSave.HasMemberWithName(memberName))
                {
                    return MembershipInfo.ContainedInThis;
                }
            }
            else if (glueState.CurrentEntitySave != null)
            {
                return glueState.CurrentEntitySave.GetMemberMembershipInfo(memberName);
            }

            return MembershipInfo.NotContained;
        }
    }
}
