using System;
using System.Collections;
using InstructionEditor;

namespace InstructionEditor.Collections
{
	/// <summary>
	/// Summary description for SpriteArrayArray.
	/// </summary>
	public class FormationArray : CollectionBase
	{
		public int Add(Formation f)
		{	
			return List.Add(f);
		}

		public void Insert(int index, Formation f)
		{
			List.Insert(index, f);
		}

		public void Remove(Formation f)
		{
			List.Remove(f);
		
		}
		public bool Contains(Formation f)
		{
			return List.Contains(f);
		}
		public Formation this[int i]
		{
			get		{return (Formation)List[i];			}
			set		{	List[i] = value;			}
		}
	}
}

