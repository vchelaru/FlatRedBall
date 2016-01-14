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
		XmlInclude(typeof(CollapseWindowSave)),
		XmlInclude(typeof(ButtonSave)),
		XmlInclude(typeof(ListBoxBaseSave)),
		XmlInclude(typeof(ListBoxSave)),
		XmlInclude(typeof(CollapseListBoxSave)),
		XmlInclude(typeof(ColorDisplaySave)),
		XmlInclude(typeof(ComboBoxSave)),
		XmlInclude(typeof(TimeLineSave)),
		XmlInclude(typeof(MarkerTimeLineSave)),
		XmlInclude(typeof(ScrollBarSave)),
		XmlInclude(typeof(TextBoxSave)),
		XmlInclude(typeof(TextDisplaySave)),
		XmlInclude(typeof(ToggleButtonSave)),
		XmlInclude(typeof(UpDownSave)),
		XmlInclude(typeof(Vector3DisplaySave))
	]
	public class WindowSave
	{
		#region Fields

		public static bool UseVisible = false;
		public static bool ApplyVisible = true;
		public static bool ApplyMinimumScales = true;
		public float TextureLeft;
		public float TextureRight;
		public float TextureTop;
		public float TextureBottom;
		public float ScaleX;
		public float ScaleY;
		public float ScaleXVelocity;
		public float ScaleYVelocity;
		public bool MovesWhenGrabbed;
		public float WorldUnitX;
		public float WorldUnitY;
		public float WorldUnitRelativeX;
		public float WorldUnitRelativeY;
		public float X;
		public float Y;
		public bool Enabled;
		public bool HasCloseButton;
		public bool HasMoveBar;
		public string Parent;
		public bool Visible;
		public string Name;
		public float MinimumScaleX;
		public float MinimumScaleY;
		public float MaximumScaleX;
		public float MaximumScaleY;
		public bool Resizable;
		public bool DrawBorders;
		public string BaseTexture;
		public float Alpha;
		public bool IgnoredByCursor;

		#endregion

		#region Methods

		public static WindowSave FromFile(string fileName)
		{
			return FileManager.XmlDeserialize<WindowSave>(fileName);
		}

		public static T FromRuntime<T>(FlatRedBall.Gui.Window runtimeInstance) where T : WindowSave, new()
		{
			T saveInstance = new T();

			saveInstance.TextureLeft = runtimeInstance.TextureLeft;
			saveInstance.TextureRight = runtimeInstance.TextureRight;
			saveInstance.TextureTop = runtimeInstance.TextureTop;
			saveInstance.TextureBottom = runtimeInstance.TextureBottom;
			saveInstance.ScaleX = runtimeInstance.ScaleX;
			saveInstance.ScaleY = runtimeInstance.ScaleY;
			saveInstance.ScaleXVelocity = runtimeInstance.ScaleXVelocity;
			saveInstance.ScaleYVelocity = runtimeInstance.ScaleYVelocity;
			saveInstance.MovesWhenGrabbed = runtimeInstance.MovesWhenGrabbed;
			saveInstance.WorldUnitX = runtimeInstance.WorldUnitX;
			saveInstance.WorldUnitY = runtimeInstance.WorldUnitY;
			saveInstance.WorldUnitRelativeX = runtimeInstance.WorldUnitRelativeX;
			saveInstance.WorldUnitRelativeY = runtimeInstance.WorldUnitRelativeY;
			saveInstance.X = runtimeInstance.X;
			saveInstance.Y = runtimeInstance.Y;
			saveInstance.Enabled = runtimeInstance.Enabled;
			saveInstance.HasCloseButton = runtimeInstance.HasCloseButton;
			saveInstance.HasMoveBar = runtimeInstance.HasMoveBar;
			if (runtimeInstance.Parent != null)
			{
				saveInstance.Parent = runtimeInstance.Parent.Name;
			}
			saveInstance.Visible = runtimeInstance.Visible;
			if (runtimeInstance.Name != null)
			{
				saveInstance.Name = runtimeInstance.Name;
			}
			saveInstance.MinimumScaleX = runtimeInstance.MinimumScaleX;
			saveInstance.MinimumScaleY = runtimeInstance.MinimumScaleY;
			saveInstance.MaximumScaleX = runtimeInstance.MaximumScaleX;
			saveInstance.MaximumScaleY = runtimeInstance.MaximumScaleY;
			saveInstance.Resizable = runtimeInstance.Resizable;
			saveInstance.DrawBorders = runtimeInstance.DrawBorders;
			if (runtimeInstance.BaseTexture != null)
			{
				saveInstance.BaseTexture = runtimeInstance.BaseTexture.Name;
			}
			saveInstance.Alpha = runtimeInstance.Alpha;
			saveInstance.IgnoredByCursor = runtimeInstance.IgnoredByCursor;
			return saveInstance;
		}

		protected T ToRuntime<T>(string contentManagerName, FlatRedBall.Gui.Cursor cursor) where T : FlatRedBall.Gui.Window
		{
			ConstructorInfo constructorInfo = 
			  typeof(T).GetConstructor(new Type[] { typeof(FlatRedBall.Gui.Cursor) });
			T runtimeInstance = (T)constructorInfo.Invoke(new object[]{cursor});
			return runtimeInstance;

		}
		public void SetRuntime(FlatRedBall.Gui.Window runtimeInstance, string contentManagerName)
		{
			runtimeInstance.TextureLeft = this.TextureLeft;
			runtimeInstance.TextureRight = this.TextureRight;
			runtimeInstance.TextureTop = this.TextureTop;
			runtimeInstance.TextureBottom = this.TextureBottom;
			runtimeInstance.ScaleX = this.ScaleX;
			runtimeInstance.ScaleY = this.ScaleY;
			runtimeInstance.ScaleXVelocity = this.ScaleXVelocity;
			runtimeInstance.ScaleYVelocity = this.ScaleYVelocity;
			runtimeInstance.MovesWhenGrabbed = this.MovesWhenGrabbed;
			runtimeInstance.WorldUnitX = this.WorldUnitX;
			runtimeInstance.WorldUnitY = this.WorldUnitY;
			runtimeInstance.WorldUnitRelativeX = this.WorldUnitRelativeX;
			runtimeInstance.WorldUnitRelativeY = this.WorldUnitRelativeY;
			runtimeInstance.X = this.X;
			runtimeInstance.Y = this.Y;
			runtimeInstance.Enabled = this.Enabled;
			runtimeInstance.HasCloseButton = this.HasCloseButton;
			runtimeInstance.HasMoveBar = this.HasMoveBar;
			if (UseVisible) 
			{
				runtimeInstance.Visible = this.Visible;
			}
			runtimeInstance.Name = this.Name;
			runtimeInstance.MinimumScaleX = this.MinimumScaleX;
			runtimeInstance.MinimumScaleY = this.MinimumScaleY;
			runtimeInstance.MaximumScaleX = this.MaximumScaleX;
			runtimeInstance.MaximumScaleY = this.MaximumScaleY;
			runtimeInstance.Resizable = this.Resizable;
			runtimeInstance.DrawBorders = this.DrawBorders;
			if (this.BaseTexture != null)
			{
				runtimeInstance.BaseTexture = FlatRedBallServices.Load<Texture2D>(this.BaseTexture, contentManagerName);
			}
			runtimeInstance.Alpha = this.Alpha;
			runtimeInstance.IgnoredByCursor = this.IgnoredByCursor;
		}

		public FlatRedBall.Gui.Window ToRuntime(string contentManagerName, FlatRedBall.Gui.Cursor cursor)
		{
			FlatRedBall.Gui.Window runtimeInstance = ToRuntime<FlatRedBall.Gui.Window>(contentManagerName, cursor);
			SetRuntime(runtimeInstance, contentManagerName);
			return runtimeInstance;
		}

		public void Save(string fileName)
		{
			FileManager.XmlSerialize(this, fileName);
		}

		public void SetFloatingChildren(FlatRedBall.Gui.Window runtimeInstance, List<Window> windowList)
		{
			throw new NotImplementedException();
		}

		public void SetChildren(FlatRedBall.Gui.Window runtimeInstance, List<Window> windowList)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
