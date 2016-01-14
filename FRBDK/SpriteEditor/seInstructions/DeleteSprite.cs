using System;
using FlatRedBall;
using FlatRedBall.Instructions;

namespace FlatRedBall
{
	/// <summary>
	/// SpriteEditor instruction for deleting a Sprite.  This is created whenever a Sprite is deleted so it can be undone.
	/// </summary>
	public class DeleteSprite : FrbInstruction
	{
        /// <summary>
        /// The reference Sprite is cloned here because some things may change on the Sprite after it is deleted; 
        /// specifically its attachments.
        /// </summary>
        /// <param name="referenceSprite">Reference to the Sprite that is being deleted.</param>
		public DeleteSprite(Sprite referenceSprite)
		{
			 referenceObject = referenceSprite.Clone();

			for(int i = 0; i < referenceSprite.Children.Count; i++)
				((Sprite)referenceObject).Children.AddOneWay(referenceSprite.Children[i]);
			
			
		}

		public override string ToString()
		{
			return base.ToString() + ": " + ((Sprite)referenceObject).Name;
		}

	}
}
