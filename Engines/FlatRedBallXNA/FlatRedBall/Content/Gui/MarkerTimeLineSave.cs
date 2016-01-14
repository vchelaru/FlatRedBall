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
	public class MarkerTimeLineSave : TimeLineSave
	{
		#region Fields


		#endregion

		#region Methods

		public static new MarkerTimeLineSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<MarkerTimeLineSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.MarkerTimeLine runtimeInstance) where T : MarkerTimeLineSave, new()
		{
			T saveInstance = TimeLineSave.FromRuntime<T>(runtimeInstance);

			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.MarkerTimeLine
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.MarkerTimeLine runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
		}

		public new FlatRedBall.Gui.MarkerTimeLine ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.MarkerTimeLine runtimeInstance = ToRuntime<FlatRedBall.Gui.MarkerTimeLine>(contentManagerName, cursor);
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
