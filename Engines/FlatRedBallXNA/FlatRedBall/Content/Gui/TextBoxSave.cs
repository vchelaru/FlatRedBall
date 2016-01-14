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
	public class TextBoxSave : WindowSave
	{
		#region Fields

		public FlatRedBall.Gui.TextBox.FormatTypes Format;
		public bool fixedLength;
		public FlatRedBall.Graphics.HorizontalAlignment Alignment;
		public bool HideCharacters;
		public float Spacing;
		public float Scale;
		public string Text;
		public bool TakingInput;

		#endregion

		#region Methods

		public static new TextBoxSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<TextBoxSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.TextBox runtimeInstance) where T : TextBoxSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.Format = runtimeInstance.Format;
			saveInstance.fixedLength = runtimeInstance.fixedLength;
			saveInstance.Alignment = runtimeInstance.Alignment;
			saveInstance.HideCharacters = runtimeInstance.HideCharacters;
			saveInstance.Spacing = runtimeInstance.Spacing;
			saveInstance.Scale = runtimeInstance.Scale;
			if (runtimeInstance.Text != null)
			{
				saveInstance.Text = runtimeInstance.Text;
			}
			saveInstance.TakingInput = runtimeInstance.TakingInput;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.TextBox
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.TextBox runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.Format = this.Format;
			runtimeInstance.fixedLength = this.fixedLength;
			runtimeInstance.Alignment = this.Alignment;
			runtimeInstance.HideCharacters = this.HideCharacters;
			runtimeInstance.Spacing = this.Spacing;
			runtimeInstance.Scale = this.Scale;
			runtimeInstance.Text = this.Text;
			runtimeInstance.TakingInput = this.TakingInput;
		}

		public new FlatRedBall.Gui.TextBox ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.TextBox runtimeInstance = ToRuntime<FlatRedBall.Gui.TextBox>(contentManagerName, cursor);
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
