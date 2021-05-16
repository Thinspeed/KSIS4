using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSIS4
{
    class BlackList
    {
        FileInfo file;

        public BlackList(string path)
        {
            file = new FileInfo(path);
        }

        public bool Contains(string url)
        {
            lock(file)
            {
                using (var stream = file.OpenText())
                {
                    var content = stream.ReadToEnd();
                    if (content.Contains(url))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
