
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
        static string _mappedTemp = ""; // this must be in umbraco somewhere...

        static FileHelper()
        {
            _mappedRoot = IOHelper.MapPath(uSyncContentSettings.Folder);
            _mappedArchive = IOHelper.MapPath(uSyncContentSettings.ArchiveFolder);
            _mappedTemp = IOHelper.MapPath("~/App_data/Temp/"); 

            if (!Directory.Exists(_mappedRoot))
                Directory.CreateDirectory(_mappedRoot);
        }

        public static string uSyncRoot
        {
            get { return _mappedRoot; }
        }

        public static string uSyncTemp
        {
            get { return _mappedTemp; }
        }


        public static bool SaveContentFile(string path, IContent node, XElement element)
        {
            LogHelper.Debug<FileHelper>("SaveContentFile {0} {1}", () => path, () => node.Name);

            string filename = string.Format("{0}.content", CleanFileName(node.Name));

            string fullpath = Path.Combine(string.Format("{0}{1}", _mappedRoot, path), filename);

            if (!Directory.Exists(Path.GetDirectoryName(fullpath)))
                Directory.CreateDirectory(Path.GetDirectoryName(fullpath));

            if (System.IO.File.Exists(fullpath))
                System.IO.File.Delete(fullpath);

            element.Save(fullpath); 

            return true; 
        }

        public static void ArchiveContentFile(string path, IContent node)
        {
            LogHelper.Debug<FileHelper>("Archiving. {0} {1}", ()=> path, ()=> node.Name); 

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

        /// <summary>
        ///  renames a content node and any child folder it may have
        /// </summary>
        public static void RenameContentFile(string path, IContent node, string oldName)
        {
            LogHelper.Debug<FileHelper>("Rename {0} {1}", () => path, () => oldName);
            
            string folderRoot = String.Format("{0}{1}", _mappedRoot, path );
 
            string oldFile = Path.Combine( folderRoot, 
                string.Format("{0}.content", CleanFileName(oldName)));

            // we just delete it, because save is fired to create the new one.
            if (System.IO.File.Exists(oldFile))
                System.IO.File.Delete(oldFile); 

            string oldFolder = Path.Combine(folderRoot, CleanFileName(oldName));
            string newFolder = Path.Combine(folderRoot, CleanFileName(node.Name));

            if ( Directory.Exists(oldFolder) )
                Directory.Move(oldFolder, newFolder) ; 
        }

        public static void MoveContentFile(string oldPath, string newPath, string name)
        {
            LogHelper.Debug<FileHelper>("Move\n Old: {0}\n New: {1}\n Name: {2}", () => oldPath, () => newPath, () => name);

            string oldRoot = String.Format("{0}{1}", _mappedRoot, oldPath);
            string newFolder = String.Format("{0}{1}", _mappedRoot, newPath);

            string oldFile = Path.Combine(oldRoot, String.Format("{0}.content", CleanFileName(name)));

            
            if (System.IO.File.Exists(oldFile))
                System.IO.File.Delete(oldFile);
            
            string oldFolder = Path.Combine(oldRoot, CleanFileName(name));
            
            if (Directory.Exists(oldFolder))
            {
                if (!Directory.Exists(Path.GetDirectoryName(newFolder)))
                    Directory.CreateDirectory(Path.GetDirectoryName(newFolder));

                Directory.Move(oldFolder, newFolder);
            }
        }

        public static string CleanFileName(string name)
        {
            return name.ToSafeAliasWithForcingCheck(); 
        }

    }
}
