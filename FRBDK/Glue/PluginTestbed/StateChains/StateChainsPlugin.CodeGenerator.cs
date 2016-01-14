using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace PluginTestbed.StateChains
{
    [Export(typeof(ICodeGeneratorPlugin))]
    public partial class StateChainsPlugin : ICodeGeneratorPlugin
    {
        private readonly List<ElementComponentCodeGenerator> _codeGeneratorList =
            new List<ElementComponentCodeGenerator>();
    
        private void InitCodeGen()
        {
            _codeGeneratorList.Add(new CodeGeneratorStateChain {ParentPlugin = this});
        }

        public void CodeGenerationStart(IElement element)
        {
            var stateChainCollection = GlueCommands.TreeNodeCommands.GetProperty<StateChainCollection>(element, PropertyName);

            if(stateChainCollection == null) return;

            var elementNameWithoutPath = FileManager.RemovePath(element.Name);
            var document = new CodeDocument();
            ICodeBlock codeBlock = document;


            if (stateChainCollection.StateChains.Count <= 0)
            {
                return;
            }


            codeBlock = codeBlock
                .Line("using FlatRedBall.Instructions;")
                .Namespace(GlueCommands.GenerateCodeCommands.GetNamespaceForElement(element))
                    .Class("public partial ", element.ClassName, "");


            //Create Enum
            codeBlock = codeBlock
                .Enum("public", "StateChains")
                    .Line("None = 0,");

            for (int i = 0; i < stateChainCollection.StateChains.Count; i++)
            {
                if (i == stateChainCollection.StateChains.Count - 1)
                    codeBlock.Line(stateChainCollection.StateChains[i].Name);
                else
                    codeBlock.Line(stateChainCollection.StateChains[i].Name + ",");
            }

            codeBlock = codeBlock.End();

            //Private members
            codeBlock
                ._()
                .Line("private StateChains _currentStateChain = StateChains.None;")
                .Line("private int _index;")
                .Line("private Instruction _instruction;")
                ._();

            //CurrentStateChain Property
            codeBlock = codeBlock
                .Property("public StateChains", "CurrentStateChain")
                    .Get()
                        .Line("return _currentStateChain;")
                    .End()
                    .Set()
                        .Line("StopStateChain();")
                        ._()
                        .Line("_currentStateChain = value;")
                        .Line("_index = 0;")
                        ._()
                        .Switch("_currentStateChain");

            foreach (var stateChain in stateChainCollection.StateChains)
            {
                codeBlock
                    .Case("StateChains." + stateChain.Name)
                        .Line("StartNextState" + stateChain.Name + "();");
            }

            codeBlock = codeBlock
                        .End()
                    .End()
                .End();

            codeBlock._();

            //ManageStateChains
            codeBlock = codeBlock
                .Function("public void", "ManageStateChains", "")
                    .If("CurrentStateChain == StateChains.None")
                        .Line("return;")
                    .End()
                    ._()
                    .Switch("CurrentStateChain");

            foreach (var stateChain in stateChainCollection.StateChains)
            {
                var index = 0;

                codeBlock = codeBlock
                    .Case("StateChains." + stateChain.Name);

                foreach (var stateChainState in
                    stateChain.StateChainStates.Where(stateChainState => !string.IsNullOrEmpty(stateChainState.State)))
                {
                    if (index == 0)
                    {
                        codeBlock
                            .If("_index == 0 && CurrentState == VariableState." + stateChainState.State)
                            .Line("_index++;")
                            .Line("StartNextState" + stateChain.Name + "();");
                    }
                    else
                    {
                        codeBlock
                            .ElseIf("_index == " + index + " && CurrentState == VariableState." +
                                    stateChainState.State)
                            .Line("_index++;")
                            .Line("StartNextState" + stateChain.Name + "();");
                    }

                    index++;
                }

                codeBlock = codeBlock
                    .End();
            }

            codeBlock = codeBlock
                        .End()
                    .End();

            codeBlock._();

            //StopStateChain
            codeBlock = codeBlock
                .Function("public void", "StopStateChain", "")
                    .If("CurrentStateChain == StateChains.None")
                        .Line("return;")
                    .End()
                    ._()
                    .Switch("CurrentStateChain");

            foreach (var stateChain in stateChainCollection.StateChains)
            {
                var index = 0;

                codeBlock = codeBlock
                    .Case("StateChains." + stateChain.Name);

                foreach (var stateChainState in stateChain.StateChainStates)
                {
                    if (index == 0)
                    {
                        codeBlock
                            .If("_index == 0")
                                .Line("Instructions.Remove(_instruction);")
                                .Line("StopStateInterpolation(VariableState." + stateChainState.State + ");")
                            .End();
                    }else
                    {
                        codeBlock
                            .ElseIf("_index == " + index)
                                .Line("Instructions.Remove(_instruction);")
                                .Line("StopStateInterpolation(VariableState." + stateChainState.State + ");")
                            .End();
                    }

                    index++;
                }

                codeBlock = codeBlock
                    .End();
            }

            codeBlock = codeBlock
                        .End()
                    .Line("_instruction = null;")
                .End();

            codeBlock._();

            //StartNextState*****
            foreach (var stateChain in stateChainCollection.StateChains)
            {
                codeBlock = codeBlock
                    .Function("private void", "StartNextState" + stateChain.Name, "")
                        .If("_index < 0")
                            .Line("_index = 0;")
                        .End()
                        ._()
                        .If("_index >= " + stateChain.StateChainStates.Count)
                            .Line("_index = 0;")
                        .End()
                        ._()
                        .Switch("_index");

                var index = 0;

                foreach (var stateChainState in stateChain.StateChainStates)
                {
                    codeBlock
                        .Case(index.ToString())
                            .Line("_instruction = InterpolateToState(VariableState." + stateChainState.State + ", " + stateChainState.Time / 1000 + ");");

                    index++;
                }

                codeBlock = codeBlock
                        .End()
                    .End()
                    ._();
            }

            GlueCommands.ProjectCommands.CreateAndAddPartialFile(element, "StateChains", document.ToString());
        }

        public IEnumerable<ElementComponentCodeGenerator> CodeGeneratorList
        {
            get { return _codeGeneratorList; }
        }
    }
}
