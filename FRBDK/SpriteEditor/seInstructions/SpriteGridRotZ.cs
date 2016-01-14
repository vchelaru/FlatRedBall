using System;
using FlatRedBall.Instructions;
using FlatRedBall.MSG;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for SpriteGridRotationZ.
	/// </summary>
	public class SpriteGridRotationZ : FrbInstruction
	{
		float RotationZ;
		public SpriteGridRotationZ(SpriteGrid referenceGrid, float RotationZ, long TimeToExecute)
		{
			this.referenceObject = referenceGrid;			this.timeToExecute = TimeToExecute;
			RotationZ = RotationZ;
		}
		public override void Execute()
		{	
			((SpriteGrid)referenceObject).blueprint.RotationZ = RotationZ;
			((SpriteGrid)referenceObject).UpdateRotAndScl();
		}

	}
}
