namespace PluginTestbed.StateChains
{
    public class StateChainState
    {
        public string State { get; set; }
        public int Time { get; set; }

        public override string ToString()
        {
            return State + " - " + Time;
        }
    }
}
