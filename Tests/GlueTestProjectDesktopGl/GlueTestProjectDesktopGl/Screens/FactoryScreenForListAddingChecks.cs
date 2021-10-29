using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Instructions;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Localization;

using GlueTestProject.TestFramework;

namespace GlueTestProject.Screens
{
	public partial class FactoryScreenForListAddingChecks
	{

		void CustomInitialize()
		{
            var instance = Factories.DrivedNotPooledFromBaseNotPooledFactory.CreateNew();

            var contains =
                this.BaseNotPooledToSeeIfDerivedFactoryAddsToThis.Contains(instance);
            contains.ShouldBe(true, "because derived entities should automatically add to all base entity lists as of mid Dec 2018");

            instance.Destroy();
        }
        

		void CustomActivity(bool firstTimeCalled)
		{

            IsActivityFinished = true;
		}

		void CustomDestroy()
		{

            var instance = Factories.DrivedNotPooledFromBaseNotPooledFactory.CreateNew();

            var contains =
                this.BaseNotPooledToSeeIfDerivedFactoryAddsToThis.Contains(instance);
            contains.ShouldBe(false,
                "because destroy should remove all lists entities should automatically add to all base entity lists as of mid Dec 2018");

            instance.Destroy();

        }

        static void CustomLoadStaticContent(string contentManagerName)
        {


        }

	}
}
