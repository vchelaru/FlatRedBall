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
	public class ListBoxSave : ListBoxBaseSave
	{
		#region Fields

		public bool ctrlClickOn;
		public float LeftBorderWidth;

		#endregion

		#region Methods

		public static new ListBoxSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ListBoxSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.ListBox runtimeInstance) where T : ListBoxSave, new()
		{
			T saveInstance = ListBoxBaseSave.FromRuntime<T>(runtimeInstance);

			saveInstance.ctrlClickOn = runtimeInstance.ctrlClickOn;
			saveInstance.LeftBorderWidth = runtimeInstance.LeftBorderWidth;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.ListBox
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.ListBox runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.ctrlClickOn = this.ctrlClickOn;
			runtimeInstance.LeftBorderWidth = this.LeftBorderWidth;
		}

		public new FlatRedBall.Gui.ListBox ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.ListBox runtimeInstance = ToRuntime<FlatRedBall.Gui.ListBox>(contentManagerName, cursor);
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
