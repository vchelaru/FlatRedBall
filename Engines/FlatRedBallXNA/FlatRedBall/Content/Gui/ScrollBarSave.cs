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
	public class ScrollBarSave : WindowSave
	{
		#region Fields

		public FlatRedBall.Gui.ScrollBar.ScrollBarAlignment Alignment;
		public float RatioDown;
		public System.Double Sensitivity;
		public System.Double View;

		#endregion

		#region Methods

		public static new ScrollBarSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ScrollBarSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.ScrollBar runtimeInstance) where T : ScrollBarSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.Alignment = runtimeInstance.Alignment;
			saveInstance.RatioDown = runtimeInstance.RatioDown;
			saveInstance.Sensitivity = runtimeInstance.Sensitivity;
			saveInstance.View = runtimeInstance.View;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.ScrollBar
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.ScrollBar runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.Alignment = this.Alignment;
			runtimeInstance.RatioDown = this.RatioDown;
			runtimeInstance.Sensitivity = this.Sensitivity;
			runtimeInstance.View = this.View;
		}

		public new FlatRedBall.Gui.ScrollBar ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.ScrollBar runtimeInstance = ToRuntime<FlatRedBall.Gui.ScrollBar>(contentManagerName, cursor);
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
