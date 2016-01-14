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
	public class ComboBoxSave : WindowSave
	{
		#region Fields

		public bool AllowTypingInTextBox;
		public bool ExpandOnTextBoxClick;
		public bool HighlightOnRollOver;
		public FlatRedBall.Gui.ListBoxBase.Sorting SortingStyle;
		public string Text;

		#endregion

		#region Methods

		public static new ComboBoxSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<ComboBoxSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.ComboBox runtimeInstance) where T : ComboBoxSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.AllowTypingInTextBox = runtimeInstance.AllowTypingInTextBox;
			saveInstance.ExpandOnTextBoxClick = runtimeInstance.ExpandOnTextBoxClick;
			saveInstance.HighlightOnRollOver = runtimeInstance.HighlightOnRollOver;
			saveInstance.SortingStyle = runtimeInstance.SortingStyle;
			if (runtimeInstance.Text != null)
			{
				saveInstance.Text = runtimeInstance.Text;
			}
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.ComboBox
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.ComboBox runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.AllowTypingInTextBox = this.AllowTypingInTextBox;
			runtimeInstance.ExpandOnTextBoxClick = this.ExpandOnTextBoxClick;
			runtimeInstance.HighlightOnRollOver = this.HighlightOnRollOver;
			runtimeInstance.SortingStyle = this.SortingStyle;
			runtimeInstance.Text = this.Text;
		}

		public new FlatRedBall.Gui.ComboBox ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.ComboBox runtimeInstance = ToRuntime<FlatRedBall.Gui.ComboBox>(contentManagerName, cursor);
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
