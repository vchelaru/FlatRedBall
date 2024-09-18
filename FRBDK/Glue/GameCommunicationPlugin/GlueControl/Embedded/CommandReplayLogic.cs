{CompilerDirectives}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall;
using GlueControl.Dtos;
using GlueControl.Runtime;

namespace GlueControl
{
    internal class CommandReplayLogic
    {
        public static void ApplyEditorCommandsToNewElement(PositionedObject newEntity, string elementNameGlue)
        {
            var element = GlueControl.Managers.ObjectFinder.Self.GetElement(elementNameGlue);
            HashSet<string> allElementNames = new HashSet<string>();
            allElementNames.Add(elementNameGlue);

            if (element != null)
            {
                var baseElements = GlueControl.Managers.ObjectFinder.Self.GetAllBaseElementsRecursively(element);
                foreach (var baseElement in baseElements)
                {
                    allElementNames.Add(baseElement.Name);
                }
            }

            List<object> dtosToReplay = CommandReceiver.GlobalGlueToGameCommands
                .Where(item =>
                {
                    if (item is Dtos.AddObjectDto addObjectDtoReplay)
                    {
                        return allElementNames.Contains(addObjectDtoReplay.ElementNameGlue);
                    }
                    else if (item is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                    {
                        return allElementNames.Contains(glueVariableSetDataRerun.ElementNameGlue);
                    }
                    else if (item is RemoveObjectDto removeObjectDtoRerun)
                    {
                        // We'll loop through the individuals inside this dto, so for now assume true
                        return true;
                    }
                    return false;
                }).ToList();

            for (int i = 0; i < dtosToReplay.Count; i++)
            {
                var dto = dtosToReplay[i];
                if (dto is Dtos.AddObjectDto addObjectDtoRerun)
                {
                    InstanceLogic.Self.HandleCreateInstanceCommandFromGlue(addObjectDtoRerun, newEntity);
                }
                else if (dto is Dtos.GlueVariableSetData glueVariableSetDataRerun)
                {
                    // do the variables set on the entity level first, then later the ones set at screen level since
                    // screen level should always be applied later
                    GlueControl.Editing.VariableAssignmentLogic.SetVariable(glueVariableSetDataRerun, newEntity);
                }
                else if (dto is RemoveObjectDto removeObjectDtoRerun)
                {
                    RemoveObjectDtoResponse response = new RemoveObjectDtoResponse();

                    for (int j = 0; j < removeObjectDtoRerun.ObjectNames.Count; j++)
                    {
                        var shouldRerun = allElementNames.Contains(removeObjectDtoRerun.ElementNamesGlue[j]);

                        if (shouldRerun)
                        {
                            var objectName = removeObjectDtoRerun.ObjectNames[j];
                            var removeObjectElement = removeObjectDtoRerun.ElementNamesGlue[j];
                            InstanceLogic.Self.HandleDeleteObject(newEntity, removeObjectElement, objectName, response);
                        }
                    }
                }
            }
        }

    }
}
