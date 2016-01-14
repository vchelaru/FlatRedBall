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
	public class UpDownSave : WindowSave
	{
		#region Fields

		public float CurrentValue;
		public float MinValue;
		public float MaxValue;
		public int Precision;
		public float RoundTo;
		public float RoundToOffset;
		public float Sensitivity;
		public bool TakingInput;

		#endregion

		#region Methods

		public static new UpDownSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<UpDownSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.UpDown runtimeInstance) where T : UpDownSave, new()
		{
			T saveInstance = WindowSave.FromRuntime<T>(runtimeInstance);

			saveInstance.CurrentValue = runtimeInstance.CurrentValue;
			saveInstance.MinValue = runtimeInstance.MinValue;
			saveInstance.MaxValue = runtimeInstance.MaxValue;
			saveInstance.Precision = runtimeInstance.Precision;
			saveInstance.RoundTo = runtimeInstance.RoundTo;
			saveInstance.RoundToOffset = runtimeInstance.RoundToOffset;
			saveInstance.Sensitivity = runtimeInstance.Sensitivity;
			saveInstance.TakingInput = runtimeInstance.TakingInput;
			return saveInstance;
		}

		protected new T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.UpDown
		{
			T runtimeInstance = base.ToRuntime<T>(contentManagerName, cursor);
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.UpDown runtimeInstance, string contentManagerName)
		{
			base.SetRuntime(runtimeInstance, contentManagerName);
			runtimeInstance.CurrentValue = this.CurrentValue;
			runtimeInstance.MinValue = this.MinValue;
			runtimeInstance.MaxValue = this.MaxValue;
			runtimeInstance.Precision = this.Precision;
			runtimeInstance.RoundTo = this.RoundTo;
			runtimeInstance.RoundToOffset = this.RoundToOffset;
			runtimeInstance.Sensitivity = this.Sensitivity;
			runtimeInstance.TakingInput = this.TakingInput;
		}

		public new FlatRedBall.Gui.UpDown ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.UpDown runtimeInstance = ToRuntime<FlatRedBall.Gui.UpDown>(contentManagerName, cursor);
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
