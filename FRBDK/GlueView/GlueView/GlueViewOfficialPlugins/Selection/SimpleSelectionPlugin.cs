using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Plugin;
using System.ComponentModel.Composition;
using FlatRedBall.Glue;
using GlueView.Facades;
using GlueView.Wcf;
using FlatRedBall;
using System.Windows.Forms;
using FlatRedBall.Input;
using FlatRedBall.Glue.SaveClasses;

namespace GlueViewOfficialPlugins.Selection
{

    [Export(typeof(GlueViewPlugin))]
    public class SimpleSelectionPlugin : SimpleSelectionLogic
    {


        public SimpleSelectionPlugin() : base()
        {
            this.MouseMove += new EventHandler(OnMouseMove);

            this.Click += HandleMouseClick;
            this.RightClick += HandleRightClick;

            
            this.Update += HandleUpdate;
        }

        protected void HandleRightClick(object sender, EventArgs e)
        {
            ToolStripMenuItem item;
            var allOver = GlueViewState.Self.CursorState.GetAllElementRuntimesOver();
            mContextMenuStrip.Items.Clear();

            foreach (ElementRuntime elementRuntime in allOver)
            {
                if (!string.IsNullOrEmpty(elementRuntime.Name))
                {
                    var added = mContextMenuStrip.Items.Add(elementRuntime.Name, null, (obj, args) =>
                        {
                            string containerName = GlueViewState.Self.CurrentElement.Name;

                            string namedObjectName = elementRuntime.Name;
                            WcfManager.Self.GlueSelect(containerName, namedObjectName);
                            mIgnoreNextClick = true;
                        });

                    added.MouseEnter += (obj, args) =>
                        {
                            string containerName = GlueViewState.Self.CurrentElement.Name;

                            string namedObjectName = elementRuntime.Name;

                            if (elementRuntime != null)
                            {
                                mHighlight.CurrentElement = elementRuntime;
                                mHighlightedElementRuntime = elementRuntime;
                            }
                        };
                    
                }
            }
        }

        private void HandleUpdate(object sender, EventArgs e)
        {
            mHighlight.Activity();

            KeyboardActivity();
        }

        private void KeyboardActivity()
        {
            if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.F12) &&
                mHighlightedElementRuntime != null
                )
            {
                // go to definition!
                IElement highlightedElement = mHighlightedElementRuntime.AssociatedIElement;

                if (highlightedElement != null)
                {
                    WcfManager.Self.GlueSelectElement(highlightedElement.Name);

                }
            }
        }

        void HandleMouseClick(object sender, EventArgs e)
        {
            if (mIgnoreNextClick)
            {
                mIgnoreNextClick = false;
            }
            else
            {
                string containerName = null;
                string namedObjectName = null;

                if (GlueViewState.Self.CurrentElement != null)
                {
                    containerName = GlueViewState.Self.CurrentElement.Name;

                    if (mHighlightedElementRuntime != null && mHighlightedElementRuntime.AssociatedNamedObjectSave != null)
                    {
                        namedObjectName = mHighlightedElementRuntime.AssociatedNamedObjectSave.InstanceName;
                    }
                }

                WcfManager.Self.GlueSelect(containerName, namedObjectName);
            }
        }

        void OnMouseMove(object sender, EventArgs e)
        {
            if (mContextMenuStrip.Visible == false)
            {
                ElementRuntime elementRuntime = GlueViewState.Self.CursorState.GetElementRuntimeOver();

                mHighlight.CurrentElement = elementRuntime;
                mHighlightedElementRuntime = elementRuntime;
            }
            // do something:
        }


    }
}
