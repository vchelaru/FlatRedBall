using System;

namespace FlatRedBall.Network
{
	/// <summary>
	/// Summary description for NetSprite.
	/// </summary>
	public class NetSprite
	{

		internal Sprite sprite;

		internal int spriteID;

		internal ushort computerID;

		public NetSprite(Sprite sprite, int spriteID, ushort computerID)
		{
			this.spriteID = spriteID;
			this.sprite = sprite;
			this.computerID = computerID;

		}
	}
}
