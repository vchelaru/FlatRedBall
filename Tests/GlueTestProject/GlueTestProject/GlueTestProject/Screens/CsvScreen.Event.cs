using System;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Specialized;
using FlatRedBall.Audio;
using FlatRedBall.Screens;
using GlueTestProject.Entities;
using GlueTestProject.Entities.EntityFolder;
using GlueTestProject.Screens;
namespace GlueTestProject.Screens
{
	public partial class CsvScreen
	{
        void OnAfterGlobalCsvVariableWithEventSet (object sender, EventArgs e)
        {
            mHasCsvVariableEventBeenRaised = true;
        }

	}
}
