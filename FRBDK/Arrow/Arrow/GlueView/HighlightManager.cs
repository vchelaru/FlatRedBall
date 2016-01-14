using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Gui;
using FlatRedBall.Input;

namespace FlatRedBall.Arrow.GlueView
{
    public class HighlightManager : Singleton<HighlightManager>
    {
        ElementRuntimeHighlight mHighlight;
        ElementRuntime mHighlightedElementRuntime;
        List<ElementRuntime> mAvailableElementsForHighlight = new List<ElementRuntime>();
        public void Initialize()
        {
            mHighlight = new ElementRuntimeHighlight();
            mHighlight.FadeInAndOut = true;
        }


        public void Activity()
        {
            UpdateHighlightedElement();
            mHighlight.Activity();
        

        }

        private void UpdateHighlightedElement()
        {
        //    if (mContextMenuStrip.Visible == false)
            {
                ElementRuntime elementRuntime = GetElementRuntimeOver();

                mHighlight.CurrentElement = elementRuntime;
                mHighlightedElementRuntime = elementRuntime;
            }
        
        }

        private ElementRuntime GetElementRuntimeOver()
        {
            RefreshAvailableElements();

            mAvailableElementsForHighlight.Sort(Compare);


            if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.A))
            {
                int m = 3;
            }

            // Now we can do a forward loop and should be selecting top-to-bottom
            foreach (var element in mAvailableElementsForHighlight)
            {
                if (element.IsMouseOver(GuiManager.Cursor, element.Layer))
                {
                    return element;
                }
            }

            return null;

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

                IElement element = ArrowState.Self.CurrentGlueElement;

                int firstIndex = element.NamedObjects.IndexOf(firstLayerNos);
                int secondIndex = element.NamedObjects.IndexOf(secondLayerNos);

                return firstIndex.CompareTo(secondIndex);
            }
            throw new NotImplementedException();
        }



        private void RefreshAvailableElements()
        {
            mAvailableElementsForHighlight.Clear();

            ElementRuntime currentElement = GluxManager.CurrentElement;

            if (currentElement != null)
            {
                foreach (ElementRuntime elementRuntimeForNos in currentElement.ContainedElements)
                {
                    if (elementRuntimeForNos.Visible && elementRuntimeForNos.AssociatedNamedObjectSave.IsLayer == false)// && elementRuntimeForNos.IsMouseOver(GuiManager.Cursor, elementRuntimeForNos.Layer))
                    {
                        mAvailableElementsForHighlight.Add(elementRuntimeForNos);
                        //return elementRuntimeForNos;
                    }
                }

                foreach (ElementRuntime element in currentElement.ElementsInList)
                {
                    if (element.Visible)// && element.IsMouseOver(GuiManager.Cursor, element.Layer))
                    {
                        mAvailableElementsForHighlight.Add(element);
                        //return element;
                    }
                }
            }
        }

    }
}
