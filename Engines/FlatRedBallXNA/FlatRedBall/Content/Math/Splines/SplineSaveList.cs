using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Math.Splines;
using FlatRedBall.IO;

namespace FlatRedBall.Content.Math.Splines
{
    #region XML Docs
    /// <summary>
    /// Save class for lists of Splines. 
    /// </summary>
    #endregion
    public class SplineSaveList
    {
        #region Fields

        [XmlElementAttribute("Spline")]
        public List<SplineSave> Splines = new List<SplineSave>();

		string mFileName;

        #endregion

        #region Methods

        public void AddToList(List<Spline> listToAddTo)
        {
            for (int i = 0; i < this.Splines.Count; i++)
            {
                listToAddTo.Add(Splines[i].ToSpline());
            }

        }

        public static SplineSaveList FromFile(string fileName)
        {
            SplineSaveList ssl = FileManager.XmlDeserialize<SplineSaveList>(fileName);
			ssl.mFileName = fileName;
            return ssl;
        }

        public static SplineSaveList FromSplineList(List<Spline> splines)
        {
            SplineSaveList ssl = new SplineSaveList();

            for (int i = 0; i < splines.Count; i++)
            {
                ssl.Splines.Add(SplineSave.FromSpline(splines[i]));
            }

            return ssl;
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

		[Obsolete("Use ToSplineList")]
        public List<Spline> ToSplines()
        {
            List<Spline> listToReturn = new List<Spline>();

            AddToList(listToReturn);

            return listToReturn;
        }

		public SplineList ToSplineList()
		{
			SplineList listToReturn = new SplineList();
			listToReturn.Name = mFileName;
			AddToList(listToReturn);

			return listToReturn;
		}


        #endregion

    }
}
