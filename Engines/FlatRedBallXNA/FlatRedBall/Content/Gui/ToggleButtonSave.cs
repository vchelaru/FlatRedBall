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
	public class ToggleButtonSave : ButtonSave
	{
		#region Fields

		public bool IsPressed;

		#endregion

		#region Methods

		public static new ToggleButtonSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ToggleButtonSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.ToggleButton runtimeInstance) where T : ToggleButtonSave, new()
		{
			T saveInstance = ButtonSave.FromRuntime<T>(runtimeInstance);

			saveInstance.IsPressed = runtimeInstance.IsPressed;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.ToggleButton
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.ToggleButton runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.IsPressed = this.IsPressed;
		}

		public new FlatRedBall.Gui.ToggleButton ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.ToggleButton runtimeInstance = ToRuntime<FlatRedBall.Gui.ToggleButton>(contentManagerName, cursor);
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
