namespace FlatRedBall.Scripting
{
    public interface IIfScriptEngine
    {
        void AndGroup();
        void OrGroup();
        void EndGroup();
        void True();
    }
}
