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
		XmlInclude(typeof(ListBoxSave)),
		XmlInclude(typeof(CollapseListBoxSave))
	]
	public class ListBoxBaseSave : WindowSave
	{
		#region Fields

		public bool ShiftClickOn;
		public bool CtrlClickOn;
		public bool Lined;
		public bool AllowReordering;
		public FlatRedBall.Gui.ListBoxBase.ToolTipOption CurrentToolTipOption;
		public float DistanceBetweenLines;
		public float FirstItemDistanceFromTop;
		public bool HighlightOnRollOver;
		public bool ScrollBarVisible;
		public FlatRedBall.Gui.ListBoxBase.Sorting SortingStyle;
		public int StartAt;
		public bool StrongSelectOnHighlight;
		public bool TakingInput;

		#endregion

		#region Methods

		public static new ListBoxBaseSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ListBoxBaseSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.ListBoxBase runtimeInstance) where T : ListBoxBaseSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.ShiftClickOn = runtimeInstance.ShiftClickOn;
			saveInstance.CtrlClickOn = runtimeInstance.CtrlClickOn;
			saveInstance.Lined = runtimeInstance.Lined;
			saveInstance.AllowReordering = runtimeInstance.AllowReordering;
			saveInstance.CurrentToolTipOption = runtimeInstance.CurrentToolTipOption;
			saveInstance.DistanceBetweenLines = runtimeInstance.DistanceBetweenLines;
			saveInstance.FirstItemDistanceFromTop = runtimeInstance.FirstItemDistanceFromTop;
			saveInstance.HighlightOnRollOver = runtimeInstance.HighlightOnRollOver;
			saveInstance.ScrollBarVisible = runtimeInstance.ScrollBarVisible;
			saveInstance.SortingStyle = runtimeInstance.SortingStyle;
			saveInstance.StartAt = runtimeInstance.StartAt;
			saveInstance.StrongSelectOnHighlight = runtimeInstance.StrongSelectOnHighlight;
			saveInstance.TakingInput = runtimeInstance.TakingInput;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.ListBoxBase
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.ListBoxBase runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.ShiftClickOn = this.ShiftClickOn;
			runtimeInstance.CtrlClickOn = this.CtrlClickOn;
			runtimeInstance.Lined = this.Lined;
			runtimeInstance.AllowReordering = this.AllowReordering;
			runtimeInstance.CurrentToolTipOption = this.CurrentToolTipOption;
			runtimeInstance.DistanceBetweenLines = this.DistanceBetweenLines;
			runtimeInstance.FirstItemDistanceFromTop = this.FirstItemDistanceFromTop;
			runtimeInstance.HighlightOnRollOver = this.HighlightOnRollOver;
			runtimeInstance.ScrollBarVisible = this.ScrollBarVisible;
			runtimeInstance.SortingStyle = this.SortingStyle;
			runtimeInstance.StartAt = this.StartAt;
			runtimeInstance.StrongSelectOnHighlight = this.StrongSelectOnHighlight;
			runtimeInstance.TakingInput = this.TakingInput;
		}

		public new FlatRedBall.Gui.ListBoxBase ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.ListBoxBase runtimeInstance = ToRuntime<FlatRedBall.Gui.ListBoxBase>(contentManagerName, cursor);
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
