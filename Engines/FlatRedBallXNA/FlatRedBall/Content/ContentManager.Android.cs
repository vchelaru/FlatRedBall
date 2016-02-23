using System;
using System.Collections.Generic;
using System.IO;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Content
{
	public partial class ContentManager
	{




		public void ProcessTexturesWaitingToBeLoaded()
		{
            textureContentLoader.ProcessTexturesWaitingToBeLoaded();

        }
	}
}

