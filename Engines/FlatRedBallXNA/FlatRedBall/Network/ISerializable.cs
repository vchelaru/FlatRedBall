using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FlatRedBall.Network
{
    interface ISerializable
    {
        byte[] Serialize();

        void Deserialize(MemoryStream ms);
    }
}
