using System;
using System.Collections.Generic;
using System.Text;

using SpriteEditor.Gui;

namespace SpriteEditor
{
    public static class SpriteEditorSettings
    {
        #region Properties



        public static bool EditingSprites
        {
            get { return GuiData.ListWindow.EditingSprites; }
            set 
            { 
                GuiData.ListWindow.EditingSprites = value; 
            }
        }

        public static bool EditingSpriteFrames
        {
            get { return GuiData.ListWindow.EditingSpriteFrames; }
            set 
            { 
                GuiData.ListWindow.EditingSpriteFrames = value; 
            }
        }

        public static bool EditingSpriteGrids
        {
            get { return GuiData.ListWindow.EditingSpriteGrids; }
            set 
            { 
                GuiData.ListWindow.EditingSpriteGrids = value; 
            }
        }
        
        public static bool EditingTextures
        {
            get { return GuiData.ListWindow.EditingTextures; }
            set 
            { 
                GuiData.ListWindow.EditingTextures = value; 
            }
        }
        
        public static bool EditingModels
        {
            get { return GuiData.ListWindow.EditingModels; }
            set 
            { 
                GuiData.ListWindow.EditingModels = value; 
            }
        }

        public static bool EditingTexts
        {
            get { return GuiData.ListWindow.EditingTexts; }
            set 
            { 
                GuiData.ListWindow.EditingTexts = value; 
            }
        }

        public static bool ViewingAnimationChains
        {
            get { return GuiData.ListWindow.ViewingAnimationChains; }
            set { GuiData.ListWindow.ViewingAnimationChains = value; }
        }

        #endregion

        #region Methods

        public static void Initialize()
        {
        }

        #endregion
    }
}
