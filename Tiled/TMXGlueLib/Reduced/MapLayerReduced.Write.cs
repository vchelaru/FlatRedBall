using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TMXGlueLib.Reduced
{
    public partial class MapLayerReduced
    {
        public void WriteMapLayer(MapLayer mapLayer, BinaryWriter binaryWriter)
        {
            int count = mapLayer.data.Length;

            for (int i = 0; i < count; i++)
            {
                short shortToWrite = 1234;

                binaryWriter.Write(shortToWrite);
            }

        }
    }
}
