using FlatRedBall.AnimationEditorForms.ViewModels;
using FlatRedBall.SpecializedXnaControls.RegionSelection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Managers
{
    class WinformsCursorManager : Singleton<WinformsCursorManager>
    {
        System.Windows.Forms.Cursor addCursor;

        public WinformsCursorManager()
        {

        }

        public void Initialize(System.Windows.Forms.Cursor addCursor)
        {
            // I don't know why, but if I try to initialize the cursor here I get an exception, but if I do it in the
            // wireframe manager, all is good. Weird...
            this.addCursor = addCursor;

        }


        public System.Windows.Forms.Cursor PerformCursorUpdateLogic(InputLibrary.Keyboard keyboard,
            InputLibrary.Cursor xnaCursor,
            WireframeEditControlsViewModel WireframeEditControlsViewModel,
            List<RectangleSelector> RectangleSelectors)
        {
            if(keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.D))
            {
                int m = 3;
            }

            System.Windows.Forms.Cursor cursorToAssign = Cursors.Arrow;

            bool isCtrlDown =
                keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) ||
                keyboard.KeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);

            if (isCtrlDown && WireframeEditControlsViewModel.IsMagicWandSelected &&
                SelectedState.Self.SelectedChain != null)
            {
                cursorToAssign = addCursor;
            }
            else if (isCtrlDown && WireframeEditControlsViewModel.IsSnapToGridChecked &&
                SelectedState.Self.SelectedChain != null)
            {
                cursorToAssign = addCursor;
            }
            else
            {
                foreach (var selector in RectangleSelectors)
                {
                    var cursorFromRect = selector.GetCursorToSet(xnaCursor);

                    if (cursorFromRect != null && cursorFromRect != Cursors.Arrow)
                    {
                        cursorToAssign = cursorFromRect;
                        break;
                    }
                }
            }

            return cursorToAssign;

        }
    }
}
