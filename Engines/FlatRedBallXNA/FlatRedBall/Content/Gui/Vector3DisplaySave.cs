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
	public class Vector3DisplaySave : WindowSave
	{
		#region Fields

		public int NumberOfComponents;
		public float Sensitivity;
		public Vector2 Vector2Value;
		public Vector3 Vector3Value;

		#endregion

		#region Methods

		public static new Vector3DisplaySave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<Vector3DisplaySave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.Vector3Display runtimeInstance) where T : Vector3DisplaySave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.NumberOfComponents = runtimeInstance.NumberOfComponents;
			saveInstance.Sensitivity = runtimeInstance.Sensitivity;
			saveInstance.Vector2Value = runtimeInstance.Vector2Value;
			saveInstance.Vector3Value = runtimeInstance.Vector3Value;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.Vector3Display
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.Vector3Display runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.NumberOfComponents = this.NumberOfComponents;
			runtimeInstance.Sensitivity = this.Sensitivity;
			runtimeInstance.Vector2Value = this.Vector2Value;
			runtimeInstance.Vector3Value = this.Vector3Value;
		}

		public new FlatRedBall.Gui.Vector3Display ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.Vector3Display runtimeInstance = ToRuntime<FlatRedBall.Gui.Vector3Display>(contentManagerName, cursor);
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
