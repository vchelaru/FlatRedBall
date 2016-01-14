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
	public class CollapseListBoxSave : ListBoxBaseSave
	{
		#region Fields

		public bool ShowExpandCollapseAllOption;

		#endregion

		#region Methods

		public static new CollapseListBoxSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<CollapseListBoxSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.CollapseListBox runtimeInstance) where T : CollapseListBoxSave, new()
		{
			T saveInstance = ListBoxBaseSave.FromRuntime<T>(runtimeInstance);

			saveInstance.ShowExpandCollapseAllOption = runtimeInstance.ShowExpandCollapseAllOption;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.CollapseListBox
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.CollapseListBox runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.ShowExpandCollapseAllOption = this.ShowExpandCollapseAllOption;
		}

		public new FlatRedBall.Gui.CollapseListBox ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.CollapseListBox runtimeInstance = ToRuntime<FlatRedBall.Gui.CollapseListBox>(contentManagerName, cursor);
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
