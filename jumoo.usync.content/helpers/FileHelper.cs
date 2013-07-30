
using System.IO ; 
using Umbraco.Core.IO ; 

using Umbraco.Core ;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using System.Xml.Linq;
using System; 

namespace jumoo.usync.content.helpers
{
    public class FileHelper
    {
        static string _mappedRoot = "";
        static string _mappedArchive = ""; 

        static FileHelper()
        {
            _mappedRoot = IOHelper.MapPath(uSyncContentSettings.Folder);
            _mappedArchive = IOHelper.MapPath(uSyncContentSettings.ArchiveFolder); 

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

            LogHelper.Info(typeof(FileHelper), string.Format("Saving {0}", fullpath));

            if (!Directory.Exists(Path.GetDirectoryName(fullpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(fullpath));

            if (System.IO.File.Exists(fullpath))
                System.IO.File.Delete(fullpath);

            element.Save(fullpath); 

            return true; 
        }

        public static void ArchiveContentFile(string path, IContent node)
        {
            LogHelper.Info(typeof(FileHelper), string.Format("Archiving. {0} {1}", path, node.Name)); 

            string filename = string.Format("{0}.content", CleanFileName(node.Name));
            string fullpath = Path.Combine(string.Format("{0}{1}", _mappedRoot, path), filename);

            if ( System.IO.File.Exists(fullpath) )
            {
                if ( uSyncContentSettings.Versions ) 
                {
                    string archiveFolder = Path.Combine(string.Format("{0}{1}", _mappedArchive, path));
                    if (!Directory.Exists(archiveFolder))
                        Directory.CreateDirectory(archiveFolder);

                    string archiveFile = Path.Combine(archiveFolder, string.Format("{0}_{1}.content", CleanFileName(node.Name), DateTime.Now.ToString("ddMMyy_HHmmss")));

                    System.IO.File.Copy(fullpath, archiveFile); 
                                     
                }
                System.IO.File.Delete(fullpath); 
            }
        }

        public static string CleanFileName(string name)
        {
            return name.ToSafeAliasWithForcingCheck(); 
        }

    }
}
