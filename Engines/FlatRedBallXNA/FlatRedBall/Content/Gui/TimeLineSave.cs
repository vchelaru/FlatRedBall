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
	[
		XmlInclude(typeof(MarkerTimeLineSave))
	]
	public class TimeLineSave : WindowSave
	{
		#region Fields

		public System.Double CurrentValue;
		public bool HasScrollBar;
		public System.Double MinimumValue;
		public System.Double MaximumValue;
		public int Precision;
		public System.Double Start;
		public FlatRedBall.Gui.TimeLine.TimeUnit TimeUnitDisplayed;
		public System.Double ValueWidth;
		public System.Double VerticalBarIncrement;
		public System.Double SmallVerticalBarIncrement;
		public bool ShowValues;

		#endregion

		#region Methods

		public static new TimeLineSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<TimeLineSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.TimeLine runtimeInstance) where T : TimeLineSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.CurrentValue = runtimeInstance.CurrentValue;
			saveInstance.HasScrollBar = runtimeInstance.HasScrollBar;
			saveInstance.MinimumValue = runtimeInstance.MinimumValue;
			saveInstance.MaximumValue = runtimeInstance.MaximumValue;
			saveInstance.Precision = runtimeInstance.Precision;
			saveInstance.Start = runtimeInstance.Start;
			saveInstance.TimeUnitDisplayed = runtimeInstance.TimeUnitDisplayed;
			saveInstance.ValueWidth = runtimeInstance.ValueWidth;
			saveInstance.VerticalBarIncrement = runtimeInstance.VerticalBarIncrement;
			saveInstance.SmallVerticalBarIncrement = runtimeInstance.SmallVerticalBarIncrement;
			saveInstance.ShowValues = runtimeInstance.ShowValues;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.TimeLine
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.TimeLine runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.CurrentValue = this.CurrentValue;
			runtimeInstance.HasScrollBar = this.HasScrollBar;
			runtimeInstance.MinimumValue = this.MinimumValue;
			runtimeInstance.MaximumValue = this.MaximumValue;
			runtimeInstance.Precision = this.Precision;
			runtimeInstance.Start = this.Start;
			runtimeInstance.TimeUnitDisplayed = this.TimeUnitDisplayed;
			runtimeInstance.ValueWidth = this.ValueWidth;
			runtimeInstance.VerticalBarIncrement = this.VerticalBarIncrement;
			runtimeInstance.SmallVerticalBarIncrement = this.SmallVerticalBarIncrement;
			runtimeInstance.ShowValues = this.ShowValues;
		}

		public new FlatRedBall.Gui.TimeLine ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.TimeLine runtimeInstance = ToRuntime<FlatRedBall.Gui.TimeLine>(contentManagerName, cursor);
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
