using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using System.IO;

using FlatRedBall.Math;
using FileManager = FlatRedBall.IO.FileManager;

namespace FlatRedBall.Content.Polygon
{
    public class PolygonSaveList
    {
        #region Fields

        //[XmlElementAttribute("Polygon")]
        public List<PolygonSave> PolygonSaves = new List<PolygonSave>();

        string mFileName;

        #endregion

        #region Properties

        [XmlIgnore]
        public string FileName
        {
            get { return mFileName; }
        }

        #endregion

        #region Methods
        public static PolygonSaveList FromFile(string fileName)
        {
            PolygonSaveList polygonSaveList = 
                FileManager.XmlDeserialize<PolygonSaveList>(fileName);

            polygonSaveList.mFileName = fileName;

            return polygonSaveList;
        }

        public static PolygonSaveList FromRuntime<T>(IList<T> polygonList) where T : FlatRedBall.Math.Geometry.Polygon
        {
            PolygonSaveList psl = new PolygonSaveList();

            for (int i = 0; i < polygonList.Count; i++)
            {
                if (polygonList[i] != null)
                {
                    psl.PolygonSaves.Add(PolygonSave.FromPolygon(polygonList[i]));
                }
                else
                {
                    psl.PolygonSaves.Add(null);
                }
            }

            return psl;

        }

        public void AddPolygonList(PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> polygonsToAdd)
        {
            foreach (FlatRedBall.Math.Geometry.Polygon polygon in polygonsToAdd)
            {
                PolygonSave polygonSave = PolygonSave.FromPolygon(polygon);
                PolygonSaves.Add(polygonSave);
            }
        }


        public void Save(string fileName)
        {
            FileManager.XmlSerialize(this, fileName);
        }

        public PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> ToPolygonList()
        {
            PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> listToReturn = new PositionedObjectList<FlatRedBall.Math.Geometry.Polygon>();

            foreach (PolygonSave polygonSave in PolygonSaves)
            {
                FlatRedBall.Math.Geometry.Polygon polygon = polygonSave.ToPolygon();
                listToReturn.Add(polygon);
            }

            return listToReturn;
        }

        #endregion

    }
}
