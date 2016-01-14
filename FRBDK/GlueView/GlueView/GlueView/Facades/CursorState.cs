using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Glue.SaveClasses;

namespace GlueView.Facades
{
    public class CursorState
    {
        List<ElementRuntime> mAvailableElements = new List<ElementRuntime>();

        public ElementRuntime GetElementRuntimeOver()
        {
            RefreshAvailableElements();

            mAvailableElements.Sort(Compare);

            // Now we can do a forward loop and should be selecting top-to-bottom
            foreach (var element in mAvailableElements)
            {
                if (element.IsMouseOver(GuiManager.Cursor, element.Layer))
                {
                    return element;
                }
            }

            return null;
        }

        public List<ElementRuntime> GetAllElementRuntimesOver()
        {
            List<ElementRuntime> toReturn = new List<ElementRuntime>();
            RefreshAvailableElements();

            mAvailableElements.Sort(Compare);

            // Now we can do a forward loop and should be selecting top-to-bottom
            foreach (var element in mAvailableElements)
            {
                if (element.IsMouseOver(GuiManager.Cursor, element.Layer))
                {
                    toReturn.Add(element);
                }
            }

            return toReturn;

        }

        private void RefreshAvailableElements()
        {
            mAvailableElements.Clear();

            ElementRuntime currentElement = GluxManager.CurrentElement;

            if (currentElement != null)
            {
                foreach (ElementRuntime elementRuntimeForNos in currentElement.ContainedElements)
                {
                    if (elementRuntimeForNos.Visible && elementRuntimeForNos.AssociatedNamedObjectSave.IsLayer == false)// && elementRuntimeForNos.IsMouseOver(GuiManager.Cursor, elementRuntimeForNos.Layer))
                    {
                        mAvailableElements.Add(elementRuntimeForNos);
                        //return elementRuntimeForNos;
                    }
                }

                foreach (ElementRuntime element in GlueViewState.Self.CurrentElementRuntime.ElementsInList)
                {
                    if (element.Visible)// && element.IsMouseOver(GuiManager.Cursor, element.Layer))
                    {
                        mAvailableElements.Add(element);
                        //return element;
                    }
                }
            }
        }

        int Compare(ElementRuntime first, ElementRuntime second)
        {
            ElementRuntime currentElement = GluxManager.CurrentElement;

            NamedObjectSave mFirstNos = first.AssociatedNamedObjectSave;
            NamedObjectSave mSecondNos = second.AssociatedNamedObjectSave;

            string firstLayer = mFirstNos.LayerOn;
            string secondLayer = mSecondNos.LayerOn;

            if (string.IsNullOrEmpty(firstLayer) && !string.IsNullOrEmpty(secondLayer))
            {
                // second is not on a layer, so that should come first
                return 1;
            }
            else if (!string.IsNullOrEmpty(firstLayer) && string.IsNullOrEmpty(secondLayer))
            {
                return -1;
            }
            else if (string.IsNullOrEmpty(firstLayer))
            {
                // they both are, so compare their Z
                return -first.Z.CompareTo(second.Z);
            }
            else
            {
                // they're on separate layers, so compare the layer indexes
                IElement element = currentElement.AssociatedIElement;

                NamedObjectSave firstLayerNos = element.GetNamedObjectRecursively(firstLayer);
                NamedObjectSave secondLayerNos = element.GetNamedObjectRecursively(secondLayer);

                return -Compare(firstLayerNos, secondLayerNos);
            }
        }

        private int Compare(NamedObjectSave firstLayerNos, NamedObjectSave secondLayerNos)
        {
            if (firstLayerNos == null)
            {
                return 1;
            }
            else if (secondLayerNos == null)
            {
                return -1;
            }
            else
            {

                IElement element = GlueViewState.Self.CurrentElement;

                int firstIndex = element.NamedObjects.IndexOf(firstLayerNos);
                int secondIndex = element.NamedObjects.IndexOf(secondLayerNos);

                return firstIndex.CompareTo(secondIndex);
            }
            throw new NotImplementedException();
        }

    }
}
