using System;
using FRB;
using FRB.Collections;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SETypes
{
	/// <summary>
	/// Summary description for SpriteEditorScene.
	/// </summary>
	[Serializable()]
	public class SpriteEditorScene
	{
		public SpriteSaveArray ssa;
		public CameraSave camera;
		public bool snappingOn;
		public float pixelSize;

		public StringArrayArray spriteArrays;


		public SpriteEditorScene()
		{
			ssa = new SpriteSaveArray();
		}
		public void LoadScene(string fileName)
		{
			FileStream stream = File.Open(fileName, System.IO.FileMode.Open);
			BinaryFormatter binFormatter=new BinaryFormatter();
			SpriteEditorScene tempScene =(SpriteEditorScene)binFormatter.Deserialize(stream);
			this.camera = tempScene.camera;
			this.snappingOn = tempScene.snappingOn;
			this.pixelSize = tempScene.pixelSize;
			spriteArrays = tempScene.spriteArrays;
			ssa = tempScene.ssa;

			stream.Close();
		}
		public void SaveScene(string fileName, Camera cameraPosition, SpriteArray spriteArray, bool snapping, float PixelSize, StringArrayArray SpriteArrays, SpriteManager sprMan)
		{
			spriteArrays = SpriteArrays;
			camera = new CameraSave(cameraPosition);
			snappingOn = snapping;
			pixelSize = PixelSize;
			ssa.AddSpriteArray(spriteArray, sprMan);
			FileStream stream= File.Create(fileName);
			BinaryFormatter binFormatter = new BinaryFormatter();
			binFormatter.Serialize(stream, this);
			stream.Close();
		}
	}
}
