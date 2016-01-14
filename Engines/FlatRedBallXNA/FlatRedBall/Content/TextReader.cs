using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

// TODO: replace this with the type you want to read.
using TRead = FlatRedBall.Graphics.Text;
using FlatRedBall.Content.Saves;

namespace FlatRedBall.Content
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content
    /// Pipeline to read the specified data type from binary .xnb format.
    /// 
    /// Unlike the other Content Pipeline support classes, this should
    /// be a part of your main game project, and not the Content Pipeline
    /// Extension Library project.
    /// </summary>
    public class TextReader : ContentTypeReader<TRead>
    {
        protected override TRead Read(ContentReader input, TRead existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            ObjectReader.ReadObject<TextSave>(input);

            return existingInstance;
           
        }

        public static TextSave ReadTextSave(ContentReader input)
        {


            FlatRedBall.Content.Saves.TextSave newObject = new FlatRedBall.Content.Saves.TextSave();
            if (input.ReadBoolean())
                newObject.mFontTextureInstance = input.ReadExternalReference<Microsoft.Xna.Framework.Graphics.Texture2D>();
            newObject.FontPatternText = input.ReadString();
            newObject.X = input.ReadSingle();
            newObject.Y = input.ReadSingle();
            newObject.Z = input.ReadSingle();
            newObject.RotationX = input.ReadSingle();
            newObject.RotationY = input.ReadSingle();
            newObject.RotationZ = input.ReadSingle();
            newObject.DisplayText = input.ReadString();
            newObject.Name = input.ReadString();
            newObject.Parent = input.ReadString();
            newObject.Scale = input.ReadSingle();
            newObject.Spacing = input.ReadSingle();
            newObject.NewLineDistance = input.ReadSingle();
            newObject.MaxWidth = input.ReadSingle();
            newObject.MaxWidthBehavior = (FlatRedBall.Graphics.MaxWidthBehavior)Enum.ToObject(typeof(FlatRedBall.Graphics.MaxWidthBehavior), (int)input.ReadInt32());
            newObject.VerticalAlignment = (FlatRedBall.Graphics.VerticalAlignment)Enum.ToObject(typeof(FlatRedBall.Graphics.VerticalAlignment), (int)input.ReadInt32());
            newObject.HorizontalAlignment = (FlatRedBall.Graphics.HorizontalAlignment)Enum.ToObject(typeof(FlatRedBall.Graphics.HorizontalAlignment), (int)input.ReadInt32());
            newObject.Visible = input.ReadBoolean();
            newObject.CursorSelectable = input.ReadBoolean();
            newObject.FontTexture = input.ReadString();
            newObject.FontFile = input.ReadString();
            newObject.Red = input.ReadSingle();
            newObject.Green = input.ReadSingle();
            newObject.Blue = input.ReadSingle();
            newObject.ColorOperation = input.ReadString();
            newObject.RelativeX = input.ReadSingle();
            newObject.RelativeY = input.ReadSingle();
            newObject.RelativeZ = input.ReadSingle();
            newObject.RelativeRotationX = input.ReadSingle();
            newObject.RelativeRotationY = input.ReadSingle();
            newObject.RelativeRotationZ = input.ReadSingle();
            return newObject;



        }
    }
}
