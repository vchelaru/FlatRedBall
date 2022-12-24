using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RenderingLibrary.Graphics;
using RenderingLibrary;

namespace FlatRedBall.AnimationEditorForms.Wireframe
{
    public class StatusTextController
    {
        SystemManagers mManagers;
        Text mText;

        public bool Visible
        {
            get
            {
                return mText.Visible;
            }
        }

        public StatusTextController(SystemManagers systemManagers)
        {
            mManagers = systemManagers;
            mText = new Text(systemManagers, "Unset Text");
            mText.RenderBoundary = false;
            systemManagers.TextManager.Add(mText);

            UpdateText();
        }

        public void UpdateText()
        {
            if (ProjectManager.Self.AnimationChainListSave == null)
            {
                mText.Visible = true;
                mText.RawText = "No AnimationChain loaded.";
            }
            else if (ProjectManager.Self.AnimationChainListSave.AnimationChains.Count == 0)
            {
                mText.Visible = true;
                mText.RawText = "This file contains no animations.  Add an animation to view it.";
            }
            else if (SelectedState.Self.SelectedChain == null)
            {
                // We don't want to do this anymore because we typically will show a texture there for users to add new animations.
                // This lets users CTRL+Click to add new animations.
            //    mText.Visible = true;
            //    mText.RawText = "Select an animation or frame to view its information.";
                mText.Visible = false;
            }
            else if (SelectedState.Self.SelectedChain.Frames.Count == 0)
            {
                // March 2018
                // We now show textures which may overlap this text, so don't show this text
                mText.Visible = false;
                //mText.RawText = "This animation contains no frames.  Add a frame to view it.";
            }
            else if (SelectedState.Self.SelectedFrame != null &&
                string.IsNullOrEmpty(SelectedState.Self.SelectedFrame.TextureName))
            {
                mText.Visible = true;
                mText.RawText = "This frame does not have an associated texture.  Set its texture to view it.";
            }
            else
            {
                mText.Visible = false;
            }

            if (mText.Visible)
            {
                AdjustTextSize();
            }
        }

        public void AdjustTextSize()
        {
            if (mText.Visible)
            {
                // We need to be zoomed to 100 so the text can be read okay:
                if (WireframeManager.Self.ZoomValue != 100)
                {
                    WireframeManager.Self.ZoomValue = 100;
                }

                mText.Width = System.Math.Max(50, mManagers.Renderer.Camera.ClientWidth - 2 * WireframeManager.Border);
                mText.Height = 400;

                // Why is this code here?
                //mManagers.Renderer.Camera.X = 0;

                
            }
        }
    }
}
