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
	public class CollapseWindowSave : WindowSave
	{
		#region Fields

		public bool isCollapsed;
		public float FullY;
		public float FullScaleY;

		#endregion

		#region Methods

		public static new CollapseWindowSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<CollapseWindowSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.CollapseWindow runtimeInstance) where T : CollapseWindowSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.isCollapsed = runtimeInstance.isCollapsed;
			saveInstance.FullY = runtimeInstance.FullY;
			saveInstance.FullScaleY = runtimeInstance.FullScaleY;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.CollapseWindow
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.CollapseWindow runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.isCollapsed = this.isCollapsed;
			runtimeInstance.FullY = this.FullY;
			runtimeInstance.FullScaleY = this.FullScaleY;
		}

		public new FlatRedBall.Gui.CollapseWindow ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.CollapseWindow runtimeInstance = ToRuntime<FlatRedBall.Gui.CollapseWindow>(contentManagerName, cursor);
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
