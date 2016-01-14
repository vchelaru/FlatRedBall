using System.Collections.Generic;

namespace PluginTestbed.StateChains
{
    public class StateChain
    {
        public string Name { get; set; }
        public List<StateChainState> StateChainStates = new List<StateChainState>();

        public override string ToString()
        {
            return Name;
        }
    }
}
