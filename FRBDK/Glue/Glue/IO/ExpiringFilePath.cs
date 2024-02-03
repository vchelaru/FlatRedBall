using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.IO
{
    public class ExpiringFilePath
    {
        public DateTimeOffset? Expiration { get; set; }
        public FilePath FilePath { get; set; }
    }
}
