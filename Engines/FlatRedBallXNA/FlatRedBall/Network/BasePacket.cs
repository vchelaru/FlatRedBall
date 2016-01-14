using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace FlatRedBall.Network
{
    [Serializable]
    public abstract class BasePacket
    {
        static BinaryFormatter sBinaryFormatter =
            new BinaryFormatter();

        static MemoryStream sStream;


        public int SizeOfPacket
        {
            get
            {
                sStream = new MemoryStream();
                sBinaryFormatter.Serialize(sStream, this);
                return (int)sStream.Length;
            }
        }

        public static BasePacket Deserialize(Stream stream)
        {
            return sBinaryFormatter.Deserialize(stream) as BasePacket;
        }

        public void Serialize(Stream stream)
        {
            sBinaryFormatter.Serialize(stream,
                this);
        }


    }
}
