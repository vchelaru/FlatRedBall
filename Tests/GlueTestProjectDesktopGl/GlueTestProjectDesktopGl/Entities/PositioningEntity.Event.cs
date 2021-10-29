using System;
using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;
using GlueTestProject.Entities;
using GlueTestProject.Screens;
namespace GlueTestProject.Entities
{
	public partial class PositioningEntity
	{
        void OnAfterCircleXSet (object sender, EventArgs e)
        {
            Circle.ForceUpdateDependencies();
            Circle.RelativeY = Circle.X;
        }

	}
}
