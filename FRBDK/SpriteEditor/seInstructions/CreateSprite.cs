using System;
using FlatRedBall;
using FlatRedBall.Instructions;

namespace SpriteEditor.seInstructions
{
	/// <summary>
	/// Summary description for CreateSprite.
	/// </summary>
	public class CreateSprite : FrbInstruction
	{
		public CreateSprite(Sprite referenceSprite)
		{
			this.referenceObject = referenceSprite;
		}
	}
}
