
using System.IO ; 
using Umbraco.Core.IO ; 

using Umbraco.Core ;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using System.Xml.Linq;
using System;
using System.Text; 

namespace jumoo.usync.content.helpers
{
    public class FileHelper
    {
        static string _mappedRoot = "";
        static string _mappedArchive = "";
        static string _mappedTemp = ""; // this must be in umbraco somewhere...
        static string _mappedMediaRoot = "";
        static string _mappedFilesRoot = "";

        static FileHelper()
        {
            _mappedRoot = IOHelper.MapPath(uSyncContentSettings.Folder);
            _mappedArchive = IOHelper.MapPath(uSyncContentSettings.ArchiveFolder);
            _mappedTemp = IOHelper.MapPath("~/App_data/Temp/");
            _mappedMediaRoot = IOHelper.MapPath(uSyncContentSettings.MediaFolder);
            _mappedFilesRoot = IOHelper.MapPath(uSyncContentSettings.Files); 

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

        public static string uSyncMediaRoot
        {
            get { return _mappedMediaRoot; }
        }



        public static bool SaveMediaFile(string path, IMedia media, XElement element)
        {
            LogHelper.Debug<FileHelper>("SaveMedia File {0} {1}", () => path, () => media.Name);

            string filename = string.Format("{0}.media", CleanFileName(media.Name));
            string fullpath = Path.Combine(string.Format("{0}{1}", _mappedMediaRoot, path), filename);

            return SaveContentBaseFile(fullpath, element); 

        }

        public static bool SaveContentFile(string path, IContent node, XElement element)
        {
            LogHelper.Debug<FileHelper>("SaveContentFile {0} {1}", () => path, () => node.Name);

            string filename = string.Format("{0}.content", CleanFileName(node.Name));
            string fullpath = Path.Combine(string.Format("{0}{1}", _mappedRoot, path), filename);

            return SaveContentBaseFile(fullpath, element) ; 
        }

        private static bool SaveContentBaseFile(string fullpath, XElement element)
        {
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

        const string validString = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" ; 

        public static string CleanFileName(string name)
        {
            // umbraco extension stips numbers from start - so a folder 2013 - has no name
            // return name.ToSafeAliasWithForcingCheck();
            //
            
            // A better scrub (Probibly should just bite the bullet for this one?)
            // return name.ReplaceMany(Path.GetInvalidFileNameChars(), ' ').Replace(" ", "");
            // return clean.ReplaceMany(extras.ToCharArray(), Char.) ; 

            //
            // a valid scrubber - keeps some consistanct with umbraco core
            //
            StringBuilder sb = new StringBuilder(); 

            for(int i = 0; i < name.Length; i++ )
            {
                if ( validString.Contains(name[i].ToString()) )
                {
                    sb.Append(name[i]);
                }
            }
            return sb.ToString(); 
        }


        public static void ExportMediaFile(string path, Guid key) 
        {
            string fileroot = IOHelper.MapPath(_mappedFilesRoot) ; 

            string folder =  Path.Combine(fileroot, key.ToString() ) ; 
            string dest = Path.Combine( folder, Path.GetFileName(path) ) ; 
            string source = IOHelper.MapPath( string.Format("~{0}", path)) ;


            LogHelper.Info<FileHelper>("Attempting Export {0} to {1}", () => source, () => dest); 

            if ( System.IO.File.Exists(source))
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (System.IO.File.Exists(dest))
                    System.IO.File.Delete(dest); 

                System.IO.File.Copy(source,dest) ; 
            }

        }

        public static void ImportMediaFile(Guid guid, IMedia item)
        {
            string fileroot = IOHelper.MapPath(_mappedFilesRoot);
            string folder = Path.Combine(fileroot, guid.ToString());

            LogHelper.Info<FileHelper>("Importing {0}", () => folder);


            if (!Directory.Exists(folder))
                return;

            foreach (var file in Directory.GetFiles(folder, "*.*"))
            {

                LogHelper.Info<FileHelper>("Import {0}", () => file);

                string filename = Path.GetFileName(file);

                FileStream s = new FileStream(file, FileMode.Open);

                item.SetValue("umbracoFile", filename, s);


            }
        }
    }
}
