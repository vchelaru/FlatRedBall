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
		XmlInclude(typeof(ToggleButtonSave))
	]
	public class ButtonSave : WindowSave
	{
		#region Fields

		public FlatRedBall.Gui.ButtonPushedState ButtonPushedState;
		public bool DrawBase;
		public bool FlipHorizontal;
		public bool FlipVertical;
		public bool HighlightOnDown;
		public bool ShowsToolTip;
		public string Text;

		#endregion

		#region Methods

		public static new ButtonSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ButtonSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.Button runtimeInstance) where T : ButtonSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.ButtonPushedState = runtimeInstance.ButtonPushedState;
			saveInstance.DrawBase = runtimeInstance.DrawBase;
			saveInstance.FlipHorizontal = runtimeInstance.FlipHorizontal;
			saveInstance.FlipVertical = runtimeInstance.FlipVertical;
			saveInstance.HighlightOnDown = runtimeInstance.HighlightOnDown;
			saveInstance.ShowsToolTip = runtimeInstance.ShowsToolTip;
			if (runtimeInstance.Text != null)
			{
				saveInstance.Text = runtimeInstance.Text;
			}
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.Button
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.Button runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.ButtonPushedState = this.ButtonPushedState;
			runtimeInstance.DrawBase = this.DrawBase;
			runtimeInstance.FlipHorizontal = this.FlipHorizontal;
			runtimeInstance.FlipVertical = this.FlipVertical;
			runtimeInstance.HighlightOnDown = this.HighlightOnDown;
			runtimeInstance.ShowsToolTip = this.ShowsToolTip;
			runtimeInstance.Text = this.Text;
		}

		public new FlatRedBall.Gui.Button ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.Button runtimeInstance = ToRuntime<FlatRedBall.Gui.Button>(contentManagerName, cursor);
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
