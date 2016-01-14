using System;
using FlatRedBall.Instructions;
using FlatRedBall.MSG;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for SpriteGridRotY.
	/// </summary>
	public class SpriteGridRotY : FrbInstruction
	{
		float RotationY;
		public SpriteGridRotY(SpriteGrid referenceGrid, float roty, long TimeToExecute)
		{
			this.referenceObject = referenceGrid;			this.timeToExecute = TimeToExecute;
			RotationY = roty;
		}
		public override void Execute()
		{	
			((SpriteGrid)referenceObject).blueprint.RotationY = RotationY;
			((SpriteGrid)referenceObject).UpdateRotAndScl();
		}

	}
}
