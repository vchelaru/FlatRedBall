using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Math.Splines
{
    #region XML Docs
    /// <summary>
    /// A List of Splines.  This inherits from a generic List of Splines, but provides
    /// extra functionality.  This is the runtime type for the .splx format.
    /// </summary>
    #endregion
    public class SplineList : List<Spline>
    {
        #region XML Docs
        /// <summary>
        /// The name of the SplineList.  Can be used to provide debug
        /// and identification information.
        /// </summary>
        #endregion
        public string Name
		{
			get;
			set;
        }

        #region XML Docs
        /// <summary>
        /// Instantiates an empty SplineList
        /// </summary>
        #endregion
        public SplineList()
			: base()
		{

        }

        #region XML Docs
        /// <summary>
        /// Instantiates a SplineList and sets its capacity.
        /// </summary>
        /// <param name="capacity"></param>
        #endregion
        public SplineList(int capacity) : base(capacity) { }

        #region XML Docs
        /// <summary>
        /// Instantiates a SplineList and populates it with the Splines contained in the argument IEnumerable.
        /// </summary>
        /// <param name="collection">The Splines to add to the newly-created SplineList.</param>
        #endregion
        public SplineList(IEnumerable<Spline> collection) : base(collection) { }

        #region XML Docs
        /// <summary>
        /// Searches for and returns the first Spline with the name matching the argument, or
        /// null if no matches are found.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The contained Spline with the matching name, or null.</returns>
        #endregion
        public Spline FindByName(string name)
		{
			for (int i = 0; i < this.Count; i++)
			{
				if (this[i].Name == name)
				{
					return this[i];
				}
			}

			return null;
		}

        public void AddToManagers()
        {
            foreach (var item in this)
            {
                item.Visible = true;
            }
        }

        public SplineList Clone()
        {

            SplineList toReturn = new SplineList();
            toReturn.Name = this.Name;

            foreach (var spline in this)
            {
                toReturn.Add(spline.Clone());
            }

            return toReturn;
        }

        public void RemoveFromManagers()
        {
            foreach (var item in this)
            {
                item.Visible = false;
            }
        }
    }
}
