using System;
using System.Collections.Generic;
using System.IO;
using FlatRedBall.IO;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Content
{
	public partial class ContentManager
	{

		List<string> texturesToLoad = new List<string>();

		void AddTexturesToLoadOnPrimaryThread(string fullAssetName)
		{
			lock (texturesToLoad)
			{
				texturesToLoad.Add (fullAssetName);
			}
		}

		public void ProcessTexturesWaitingToBeLoaded()
		{
			lock (texturesToLoad)
			{
				foreach (var assetName in texturesToLoad)
				{
					using (Stream stream = FileManager.GetStreamForFile(assetName))
					{
						var graphicsDevice = FlatRedBallServices.mGraphicsDevice;

						Texture2D texture = Texture2D.FromStream(graphicsDevice,
							stream);

						texture.Name = assetName;
					}
				}
			}
		}
	}
}

