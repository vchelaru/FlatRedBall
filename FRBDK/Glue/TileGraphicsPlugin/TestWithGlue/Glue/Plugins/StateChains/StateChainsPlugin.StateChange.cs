using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;

namespace PluginTestbed.StateChains
{
    [Export(typeof(IStateChange))]
	public partial class  StateChainsPlugin : IStateChange
	{
	    public void ReactToStateNameChange(IElement element, string oldName, string newName)
	    {
	        if(element is EntitySave)
	        {
	            var entity = element as EntitySave;

                var stateChainCollection = GlueCommands.TreeNodeCommands.GetProperty<StateChainCollection>(entity, PropertyName);

	            foreach (var stateChain in stateChainCollection.StateChains)
	            {
	                foreach (var stateChainState in stateChain.StateChainStates)
	                {
	                    if(stateChainState.State == oldName)
	                    {
	                        stateChainState.State = newName;
	                    }
	                }
	            }

                GlueCommands.TreeNodeCommands.SetProperty(entity, PropertyName, stateChainCollection);
	            _control.CurrentEntitySave = GlueCommands.TreeNodeCommands.GetSelectedEntitySave();
	        }
	    }

        public void ReactToStateRemoved(IElement element, string stateName)
        {
            if (element is EntitySave)
            {
                var entity = element as EntitySave;

                var stateChainCollection = GlueCommands.TreeNodeCommands.GetProperty<StateChainCollection>(entity, PropertyName);

                foreach (var stateChain in stateChainCollection.StateChains)
                {
                    var removeList = new List<StateChainState>();

                    foreach (var stateChainState in stateChain.StateChainStates)
                    {
                        if (stateChainState.State == stateName)
                        {
                            removeList.Add(stateChainState);
                        }
                    }

                    foreach (var stateChainState in removeList)
                    {
                        stateChain.StateChainStates.Remove(stateChainState);
                    }
                }

                GlueCommands.TreeNodeCommands.SetProperty(entity, PropertyName, stateChainCollection);
                _control.CurrentEntitySave = GlueCommands.TreeNodeCommands.GetSelectedEntitySave();
            }
        }
	}
}
