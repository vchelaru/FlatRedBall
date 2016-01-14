using System;
using FlatRedBall;
using FlatRedBall.MSG;
using FlatRedBall.Instructions;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for CreateSpriteGrid.
	/// </summary>
	public class CreateSpriteGrid : FrbInstruction
	{
		public CreateSpriteGrid(SpriteGrid referenceGrid)
		{
			this.referenceObject = referenceGrid;
		}
	}
}
