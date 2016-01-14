namespace FlatRedBall.Scripting
{
    public interface IDecisionList : IScriptDecision
    {
        void AddDecision(IScriptDecision decision);
        IDecisionList Parent { get; set; }
        bool Contains(IScriptDecision decision);
    }
}
