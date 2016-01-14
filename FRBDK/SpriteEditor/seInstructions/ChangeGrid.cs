using System;
using FlatRedBall.Instructions;
using FlatRedBall.MSG;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for ChangeGrid.
	/// </summary>
	public class ChangeGrid : FrbInstruction
	{
		float x;
		float y;
		float z;

		public ChangeGrid(SpriteGrid referenceGrid, float x, float y, float z, long TimeToExecute)
		{
			this.referenceObject = referenceGrid;			this.timeToExecute = TimeToExecute;
			this.x = x;
			this.y = y;
			this.z = z;
		}
		public override void Execute()
		{	
			((SpriteGrid)referenceObject).ChangeGrid(x, y, z);
		}

	}
}
