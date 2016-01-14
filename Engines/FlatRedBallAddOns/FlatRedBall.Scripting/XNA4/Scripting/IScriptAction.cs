namespace FlatRedBall.Scripting
{
    public interface IScriptAction
    {
        bool IsComplete();

        bool Execute();
    }
}