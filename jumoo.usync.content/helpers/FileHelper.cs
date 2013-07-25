
using System.IO ; 
using Umbraco.Core.IO ; 

using Umbraco.Core ;
using Umbraco.Core.Models;

using System.Xml.Linq; 

namespace jumoo.usync.content.helpers
{
    public class FileHelper
    {
        static string _contentRoot = "~/uSync.Content";
        static string _mappedRoot = "";

        static FileHelper()
        {
            _mappedRoot = IOHelper.MapPath(_contentRoot);

            if (!Directory.Exists(_mappedRoot))
                Directory.CreateDirectory(_mappedRoot);
        }

        public static string uSyncRoot
        {
            get { return _mappedRoot; }
        }

        public static bool SaveContentFile(string path, IContent node, XElement element)
        {
            string filename = string.Format("{0}.content", CleanFileName(node.Name));

            string fullpath = Path.Combine(string.Format("{0}{1}", _mappedRoot, path), filename);

            if (!Directory.Exists(Path.GetDirectoryName(fullpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(fullpath));

            if (System.IO.File.Exists(fullpath))
                System.IO.File.Delete(fullpath);

            element.Save(fullpath); 

            return true; 
        }

        public static string CleanFileName(string name)
        {
            return name.ToSafeAliasWithForcingCheck(); 
        }

    }
}
