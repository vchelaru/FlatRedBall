using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Collections;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;

using SpriteEditor.Gui;

namespace SpriteEditor
{
    public class SpriteFrameManager
    {

       #region delegate methods
        public void ConvertToSpriteFrameClick(Window callingWindow)
        {
            if (SpriteEditorSettings.EditingSprites && 
                GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                SpriteFrame newSpriteFrame = new SpriteFrame(
                    GameData.EditorLogic.CurrentSprites[0].Texture, SpriteFrame.BorderSides.All);
                SpriteManager.AddSpriteFrame(newSpriteFrame);

                newSpriteFrame.X = GameData.EditorLogic.CurrentSprites[0].X;
                newSpriteFrame.Y = GameData.EditorLogic.CurrentSprites[0].Y;
                newSpriteFrame.Z = GameData.EditorLogic.CurrentSprites[0].Z;

                newSpriteFrame.ScaleX = GameData.EditorLogic.CurrentSprites[0].ScaleX;
                newSpriteFrame.ScaleY = GameData.EditorLogic.CurrentSprites[0].ScaleY;

                newSpriteFrame.RotationX = GameData.EditorLogic.CurrentSprites[0].RotationX;
                newSpriteFrame.RotationY = GameData.EditorLogic.CurrentSprites[0].RotationY;
                newSpriteFrame.RotationZ = GameData.EditorLogic.CurrentSprites[0].RotationZ;

                newSpriteFrame.Alpha = GameData.EditorLogic.CurrentSprites[0].Alpha;

                newSpriteFrame.Red = GameData.EditorLogic.CurrentSprites[0].Red;
                newSpriteFrame.Green = GameData.EditorLogic.CurrentSprites[0].Green;
                newSpriteFrame.Blue = GameData.EditorLogic.CurrentSprites[0].Blue;

                newSpriteFrame.Name = GameData.EditorLogic.CurrentSprites[0].Name;

                GameData.DeleteCurrentSprites();

                SpriteEditorSettings.EditingSpriteFrames = true;

                // add code for undoing this.

                GameData.Scene.SpriteFrames.Add(newSpriteFrame);
            }
            else if ( SpriteEditorSettings.EditingSpriteFrames && 
                GameData.EditorLogic.CurrentSpriteFrames.Count != 0)
            {
                /*
                 * Need to set the invisible parent Sprite's texture because it will be used
                 * to set the new EditorSprite's texture.  We could manually do this, but it's
                 * cleaner to just call SetFromRegularSprite and have that handle everything.
                 */
               
                GameData.EditorLogic.CurrentSpriteFrames[0].CenterSprite.Texture =
                    GameData.EditorLogic.CurrentSpriteFrames[0].Texture;

                EditorSprite es = new EditorSprite();

                es.SetFromRegularSprite(GameData.EditorLogic.CurrentSpriteFrames[0].CenterSprite);
                es.Name = GameData.EditorLogic.CurrentSpriteFrames[0].Name;
                es.ScaleX = GameData.EditorLogic.CurrentSpriteFrames[0].ScaleX;
                es.ScaleY = GameData.EditorLogic.CurrentSpriteFrames[0].ScaleY;

                GameData.DeleteSpriteFrame(GameData.EditorLogic.CurrentSpriteFrames[0], true);

                SpriteEditorSettings.EditingSprites = true;

                SpriteManager.AddSprite(es);
                GameData.SetStoredAddToSE(es, GameData.EditorProperties.PixelSize);

                //GameData.ihMan.AddInstruction(new seInstructions.CreateSprite(es));

                GuiData.ToolsWindow.paintButton.Unpress();

                GameData.Cursor.ClickSprite(es);
                 
            }
        }
        #endregion


       public SpriteFrameManager()
        {


            GameData.Scene.SpriteFrames.Name = "All Sprite Frames";
        }


       public override string ToString()
        {
            System.Text.StringBuilder sb = new StringBuilder();

            sb.Append("# SpriteFrames: " + GameData.Scene.SpriteFrames.Count + "\n");

            foreach (SpriteFrame sf in GameData.Scene.SpriteFrames)
            {
               sb.Append("\n" + sf.ToString());
            }
            return sb.ToString();
        }
    }
}