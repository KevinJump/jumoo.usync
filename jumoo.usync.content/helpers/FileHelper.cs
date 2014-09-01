
using System.IO ; 
using Umbraco.Core.IO ; 

using Umbraco.Core ;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using System.Xml.Linq;
using System;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public static void ArchiveFile(string path, IContentBase node, bool media = false)
        {
            LogHelper.Debug<FileHelper>("Archiving. {0} {1}", ()=> path, ()=> node.Name);

            string _root = _mappedRoot;
            string _ext = ".content";
            if (media)
            {
                LogHelper.Debug<FileHelper>("Archiving a Media Item");
                _root = _mappedMediaRoot;
                _ext = ".media";
            }

            string filename = string.Format("{0}{1}", CleanFileName(node.Name), _ext);
            string fullpath = Path.Combine(string.Format("{0}{1}", _root, path), filename);

            if ( System.IO.File.Exists(fullpath) )
            {
                if ( uSyncContentSettings.Versions ) 
                {
                    string archiveFolder = Path.Combine(string.Format("{0}{1}", _mappedArchive, path));
                    if (!Directory.Exists(archiveFolder))
                        Directory.CreateDirectory(archiveFolder);

                    string archiveFile = Path.Combine(archiveFolder, string.Format("{0}_{1}{2}", CleanFileName(node.Name), DateTime.Now.ToString("ddMMyy_HHmmss"),_ext));

                    System.IO.File.Copy(fullpath, archiveFile); 
                                     
                }
                System.IO.File.Delete(fullpath); 
            }
        }
         

        /// <summary>
        ///  moves media and content files, when their parent id has changed
        /// </summary>
        public static void MoveFile(string oldPath, string newPath, string name, bool media = false)
        {
            LogHelper.Debug<FileHelper>("Move\n Old: {0}\n New: {1}\n Name: {2}", () => oldPath, () => newPath, () => name);

            string _root = _mappedRoot;
            string _ext = ".content";
            if (media) 
            {
                LogHelper.Debug<FileHelper>("Moving a Media Item");
                _root = _mappedMediaRoot;
                _ext = ".media";
            }

            string oldRoot = String.Format("{0}{1}", _root, oldPath);
            string newFolder = String.Format("{0}{1}", _root, newPath);

            string oldFile = Path.Combine(oldRoot, String.Format("{0}{1}", CleanFileName(name), _ext));

            
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

        public static void RenameFile(string path, IContentBase node, string oldName)
        {
            LogHelper.Info<FileHelper>("Rename {0} {1} {2}", () => path, ()=> oldName, ()=> node.GetType().Name);

            string _root = _mappedRoot ; 
            string _ext = ".content"; 
            if (node.GetType() == typeof(Media))
            {
                LogHelper.Info<FileHelper>("Renaming a Media Item"); 
                _root = _mappedMediaRoot;
                _ext = ".media"; 
            }

            string folderRoot = String.Format("{0}{1}", _root, path);

            string oldFile = Path.Combine(folderRoot,
                String.Format("{0}{1}", CleanFileName(oldName), _ext));

            if (System.IO.File.Exists(oldFile))
                System.IO.File.Delete(oldFile);

            string oldFolder = Path.Combine(folderRoot, CleanFileName(oldName));
            string newFolder = Path.Combine(folderRoot, CleanFileName(node.Name));

            if (Directory.Exists(oldFolder))
                Directory.Move(oldFolder, newFolder);

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

            string newName = name.ToSafeAliasWithForcingCheck();
            if (!string.IsNullOrEmpty(newName))
                return newName;
            
            for (int i = 0; i < name.Length; i++)
            {
                if (validString.Contains(name[i].ToString()))
                {
                    sb.Append(name[i]);
                }
            }
            return sb.ToString();
            
        }

        internal static bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}"))
                || (input.StartsWith("[") && input.EndsWith("]"));
        }

        public static void ExportMediaFile(string mediaInfo, Guid key) 
        {
            string fileroot = IOHelper.MapPath(_mappedFilesRoot) ;

            string path = string.Empty; 

            // Umbraco 7's image cropper saves umbracoFile as JSON
            if (IsJson(mediaInfo))
                path = JsonConvert.DeserializeObject<dynamic>(mediaInfo).src;
            else
                path = mediaInfo;

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

            LogHelper.Debug<FileHelper>("Importing {0}", () => folder);
            if (!Directory.Exists(folder))
                return;

            //
            // we look for an existing folder, if it's there we get the
            // file info. we only want to import changed files, as it 
            // creates directories all the time
            //

            FileInfo currentFile = null; 
            // some is it already there logic ? 
            if (item.HasProperty("umbracoFile"))
            {
                LogHelper.Debug<FileHelper>("Has umbracoFile");
                if (item.GetValue("umbracoFile") != null)
                {
                    LogHelper.Debug<FileHelper>("umbracoFile value [{0}]", () => item.GetValue("umbracoFile").ToString() );
                    string umbracoFile = item.GetValue("umbracoFile").ToString();

                    // standard uploaded the path is a string . 
                    // for Umbraco 7's image cropper it's all JSON
                    string path = umbracoFile;
                    if ( !string.IsNullOrWhiteSpace(umbracoFile))
                    {
                        if ( IsJson(umbracoFile))
                        {
                            var jsonUmbracoFile = JsonConvert.DeserializeObject<dynamic>(umbracoFile);
                            path = jsonUmbracoFile.src;
                        }
                    }

                    LogHelper.Debug<FileHelper>("path {0}", () => path);

                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        //
                        // we check it's in the media folder, because later we delete it and all it's sub folders
                        // if for some reason it was blank or mallformed we could trash the whole umbraco just here.
                        //
                        if (path.StartsWith("/media/"))
                        {
                            string f = IOHelper.MapPath(string.Format("~{0}", path));
                            if (System.IO.File.Exists(f))
                            {
                                LogHelper.Debug<FileHelper>("Getting info for {0}", () => f);
                                currentFile = new FileInfo(f);
                            }
                        }
                    }
                }
            }




            foreach (var file in Directory.GetFiles(folder, "*.*"))
            {

                LogHelper.Debug<FileHelper>("Import {0}", () => file);


                if (currentFile != null)
                {
                    // LogHelper.Debug<FileHelper>("Comparing {0} - {1}", () => currentFile.Name, () => file);
                    // do some file comparison, we only import if the new file
                    // is diffrent than the existing one..
                    if (!FilesAreEqual(currentFile, new FileInfo(file)))
                    {
                        LogHelper.Info<FileHelper>("Updating umbracoFile with {0} ", () => file);
                        string filename = Path.GetFileName(file);

                        FileStream s = new FileStream(file, FileMode.Open);
                        item.SetValue("umbracoFile", filename, s); // question this for imagecropper types... 
                        s.Close();

                        // ok so now we've gone and created a new folder, we need to delete the old on
                        if (Directory.Exists(currentFile.DirectoryName))
                        {
                            LogHelper.Info<FileHelper>("Removing old media folder location {0}", () => currentFile.DirectoryName);
                            Directory.Delete(currentFile.DirectoryName, true);
                        }
                    }
                    // LogHelper.Debug<FileHelper>("Done comparing {0} - {1}", () => currentFile.Name, () => file);
                }
                else
                {
                    // if we have no currentfile, that's because it's not been imported first time yet.
                    LogHelper.Info<FileHelper>("First time import of {0}", () => file);

                    FileStream s = new FileStream(file, FileMode.Open);
                    item.SetValue("umbracoFile", Path.GetFileName(file), s);
                    s.Close();
                }
                LogHelper.Debug<FileHelper>("<< Done Import {0}", () => file);
            }
        }

        public static void CleanMediaFiles(IMedia item)
        {
            string fileRoot = IOHelper.MapPath(_mappedFilesRoot);
            string folder = Path.Combine(fileRoot, ImportPairs.GetSourceGuid(item.Key).ToString());
            
            try
            {

                if (Directory.Exists(folder))
                {
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        System.IO.File.Delete(file);
                    }
                    Directory.Delete(folder);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<FileHelper>("Couldn't clean files folder", ex);
            }
        }

        const int BYTES_TO_READ = sizeof(Int64);

        private static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                     fs1.Read(one, 0, BYTES_TO_READ);
                     fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one,0) != BitConverter.ToInt64(two,0))
                        return false;
                }
            }

            return true;
        }
    }    
}
