using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.Utilities
{
	public static class ListHelper
	{
		public static void SortByName(this List<ScreenSave> listToSort)
		{
			listToSort.Sort(CompareScreenSaves);

		}

		public static void SortByName(this List<EntitySave> listToSort)
		{
			listToSort.Sort(CompareScreenSaves);

		}		
		
		static int CompareScreenSaves(INameable firstScreenSave, INameable secondScreenSave)
		{
			return firstScreenSave.Name.CompareTo(secondScreenSave.Name);
		}
	}
}
