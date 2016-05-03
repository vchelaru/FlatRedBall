using System;
using FlatRedBall;
using FlatRedBall.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using GlueTestProject.Entities;
using GlueTestProject.Entities.EntityFolder;
using GlueTestProject.Screens;
namespace GlueTestProject.Entities
{
	public partial class CsvEntity
	{
        void OnAfterTextureSettingsSet (object sender, EventArgs e)
        {
            if(TextureSettings != null)
            {
            	this.SpriteInstance.Texture = (Texture2D)GetFile(TextureSettings.TextureName);
            	
            
            }
        }

	}
}
