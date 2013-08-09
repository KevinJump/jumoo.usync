using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml;
using System.Xml.Linq;

using Umbraco.Core.Logging;

namespace jumoo.usync.core.Helpers
{
    public class uSyncIO 
    {
        const string uSyncRoot = "~/uSync2/" ;
        const string uSyncArchive = "~/uSync2.Archive/"; 

        public static string EnsurePathExists(string path) 
        {
            LogHelper.Info<uSyncIO>("Mapped Path {0} {1}", () => Umbraco.Core.IO.IOHelper.MapPath("~/uSync2"), () => path);

            string mappedPath = String.Format("{0}//{1}", Umbraco.Core.IO.IOHelper.MapPath("~/uSync2"), path); 
                //Path.Combine(Umbraco.Core.IO.IOHelper.MapPath("~/uSync2"), path); 

            if (!Directory.Exists(mappedPath))
                Directory.CreateDirectory(mappedPath);

            return mappedPath;
        }

        public static string EnsureFileFolderExists(string fullPath)
        {
            string path = Path.GetDirectoryName(fullPath);
            return String.Format("{0}\\{1}", EnsurePathExists(path), Path.GetFileName(fullPath));
        }

        /// <summary>
        ///  takes the bit of xml and saves it. 
        /// </summary>
        /// <param name="element">xml element</param>
        /// <param name="filename">full path to save (without usync bit)</param>
        public static void SaveXmlFile(XElement element, string filename)
        {
            string fullpath = EnsureFileFolderExists(filename);

            LogHelper.Info<uSyncIO>("Save XML {0}", () => fullpath); 
            
            element.Save(fullpath);
        }

        public static void ArchiveFile(string filepath)
        {
            string uSyncMappedRoot = Umbraco.Core.IO.IOHelper.MapPath(uSyncRoot);
            string uSyncMappedArchive = Umbraco.Core.IO.IOHelper.MapPath(uSyncArchive);

            string orginalFile = Path.Combine(uSyncMappedRoot, filepath);
            string archiveFile = Path.Combine(uSyncMappedRoot,
                string.Format("{0}_{1}.def", Path.GetFileNameWithoutExtension(filepath), DateTime.Now.ToShortDateString()));

            if (File.Exists(orginalFile))
                File.Copy(orginalFile, archiveFile);

            File.Delete(orginalFile);

            // delete the folder if it's there and if it's empty. 
            string folder = Path.GetDirectoryName(orginalFile);
            if (Directory.Exists(folder))
            {
                // is it empty
                if (Directory.GetFileSystemEntries(folder).Length == 0)
                {
                    Directory.Delete(folder);
                }

            }



        }
    }
}
