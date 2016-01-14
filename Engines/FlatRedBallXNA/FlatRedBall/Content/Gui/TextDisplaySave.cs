using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using FlatRedBall.IO;
#if FRB_XNA
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#elif FRB_MDX
using Vector2 = Microsoft.DirectX.Vector2;
using Vector3 = Microsoft.DirectX.Vector3;
#endif
using FlatRedBall.Gui;

namespace FlatRedBall.Content.Gui
{
	public class TextDisplaySave : WindowSave
	{
		#region Fields

		public string Text;

		#endregion

		#region Methods

		public static new TextDisplaySave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<TextDisplaySave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.TextDisplay runtimeInstance) where T : TextDisplaySave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			if (runtimeInstance.Text != null)
			{
				saveInstance.Text = runtimeInstance.Text;
			}
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.TextDisplay
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.TextDisplay runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.Text = this.Text;
		}

		public new FlatRedBall.Gui.TextDisplay ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.TextDisplay runtimeInstance = ToRuntime<FlatRedBall.Gui.TextDisplay>(contentManagerName, cursor);
			SetRuntime(runtimeInstance, contentManagerName);
			return runtimeInstance;
		}

		public new void Save(string fileName)
		{
			FileManager.XmlSerialize(this, fileName);
		}

		#endregion
	}
}
