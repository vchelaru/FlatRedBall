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
	public class ColorDisplaySave : WindowSave
	{
		#region Fields


		#endregion

		#region Methods

		public static new ColorDisplaySave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ColorDisplaySave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.ColorDisplay runtimeInstance) where T : ColorDisplaySave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.ColorDisplay
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.ColorDisplay runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
		}

		public new FlatRedBall.Gui.ColorDisplay ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.ColorDisplay runtimeInstance = ToRuntime<FlatRedBall.Gui.ColorDisplay>(contentManagerName, cursor);
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
