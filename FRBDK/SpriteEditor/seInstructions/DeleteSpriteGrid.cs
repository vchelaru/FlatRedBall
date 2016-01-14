using System;
using FlatRedBall;
using FlatRedBall.MSG;
using FlatRedBall.Instructions;

namespace FlatRedBall
{
	/// <summary>
	/// Summary description for DeleteSprite.
	/// </summary>
	public class DeleteSpriteGrid : FrbInstruction
	{
		public DeleteSpriteGrid(SpriteGrid referenceSpriteGrid)
		{
			referenceObject = referenceSpriteGrid.Clone();
		}
	}
}
