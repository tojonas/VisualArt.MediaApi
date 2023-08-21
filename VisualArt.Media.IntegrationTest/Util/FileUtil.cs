using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualArt.Media.IntegrationTest.Util
{
    public static class FileUtil
    {
        public static FileStream OpenRead( string name ) => File.OpenRead($"../../../Scenarios/{name}");
    }
}
