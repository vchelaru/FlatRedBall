using System;
using System.Collections.Generic;
using System.Collections;
using FlatRedBall.Gui;


namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for WindowArray.
	/// </summary>
	public class WindowArray : List<IWindow>
    {
		public void AddUnique(IWindow wi)
		{
			if(wi != null && !Contains(wi))
				Add(wi);
		}


		public void AddUnique(WindowArray wa)
		{
			foreach(IWindow w in wa)
				AddUnique(w);

		}
		

        public IWindow FindByName(string windowNameToSearchFor)
        {
            foreach (IWindow w in this)
                if (w.Name == windowNameToSearchFor)
                    return w;
            return null;
        }


        public IWindow FindWithNameContaining(string stringToSearchFor)
        {
            foreach (IWindow w in this)
                if (w.Name.Contains(stringToSearchFor))
                    return w;
            return null;
        }
		

		public bool Visible
		{
			set
            {	
                for(int i = 0; i < Count; i++)
                    this[i].Visible = value;
            }
		}
	}
}
