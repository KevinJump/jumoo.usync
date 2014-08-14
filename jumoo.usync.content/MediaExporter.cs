using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics; 

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using jumoo.usync.content.helpers;

using System.Xml;
using System.Xml.Linq; 

namespace jumoo.usync.content
{
    /// <summary>
    ///  exports media...
    /// </summary>
    public class MediaExporter
    {
        int _count; 
        public MediaExporter()
        {

        }

        public int Export()
        {
            LogHelper.Info<MediaExporter>("Exporting Content") ; 
            Stopwatch sw = Stopwatch.StartNew() ;
            _count = 0; 

            IMediaService _mediaService = ApplicationContext.Current.Services.MediaService; 

            foreach(var item in _mediaService.GetRootMedia())
            {
                Export("", item, _mediaService); 
            }

            SourceInfo.Save();

            sw.Stop();
            LogHelper.Info<MediaExporter>("Media Export Complete [{0} Seconds]", () => sw.Elapsed.TotalSeconds);

            return _count;  

        }

        public void Export(string path, IMedia item, IMediaService _mediaService)
        {
            LogHelper.Info<MediaExporter>("Exporting {0}", () => FileHelper.CleanFileName(item.Name));
            SaveMedia(item, path);

            path = String.Format("{0}\\{1}", path, FileHelper.CleanFileName(item.Name));
            foreach (var childItem in _mediaService.GetChildren(item.Id))
            {
                Export(path, childItem, _mediaService);
            }


        }

        public void SaveMedia(IMedia media)
        {
            string path = System.IO.Path.GetDirectoryName(GetMediaItemPath(media));
            SaveMedia(media, path); 
        }

        public void SaveMedia(IMedia media, string path)
        {
            XElement itemXml = ExportMedia(media);

            if (itemXml != null)
            {
                if (FileHelper.SaveMediaFile(path, media, itemXml))
                {
                    _count++;
                    SourceInfo.Add(media.Key, media.Name, media.ParentId);
                }
            }
            else
            {
                LogHelper.Info<MediaExporter>("Media xml came back as null");
            }
        }

        public XElement ExportMedia(IMedia item)
        {
            LogHelper.Debug<MediaExporter>("Exporting Media {0}", () => item.Name);

            var nodeName = FileHelper.CleanFileName(item.ContentType.Alias);

            XElement xml = uSyncXmlHelper.ExportContentBase(nodeName, item, false); 

            xml.Add(new XAttribute("parentGUID", item.Level > 1 ? item.Parent().Key : new Guid("00000000-0000-0000-0000-000000000000")));
            xml.Add(new XAttribute("mediaTypeAlias", item.ContentType.Alias));
            xml.Add(new XAttribute("path", item.Path));
            
            // helpers.FileHelper.ExportMediaFile(item.Path, ImportPairs.GetSourceGuid(item.Key)); 

            // any files here ??? this only works for umbracoFile values 
            //
            // ideally it should look for upload types, (based on alias) and then get any of them
            //
            // lets not make media any more complicated than it already is. 
            //
            foreach (var file in item.Properties.Where(p => p.Alias == "umbracoFile"))
            {
                if (file == null || file.Value == null)
                {
                    LogHelper.Debug<MediaExporter>("Media {0} doesn't have an associated file", () => item.Name);
                }
                else
                {
                    LogHelper.Info<MediaExporter>("umbraco file {0}", () => file.Value.ToString());
                    FileHelper.ExportMediaFile(file.Value.ToString(), ImportPairs.GetSourceGuid(item.Key));
                }

            }


            return xml; 
        }

        private string GetMediaItemPath(IMedia item)
        {
            string path = "" ;
            path = FileHelper.CleanFileName(item.Name) ;

            if (item.ParentId != -1)
            {
                path = String.Format("{0}\\{1}", GetMediaItemPath(item.Parent()), path);
            }
            return path; 
        
        }

        public void Rename(IMedia item, string oldName)
        {
            string path = System.IO.Path.GetDirectoryName(GetMediaItemPath(item));
            //  FileHelper.RenameMediaFile(path, item, oldName);
            // generic rename (both media and Content)
            FileHelper.RenameFile(path, item, oldName); 
        }

        public void Move(IMedia item, int oldParentId)
        {

            IMediaService _mediaService = ApplicationContext.Current.Services.MediaService; 

            IMedia oldParent = _mediaService.GetById(oldParentId);

            if (oldParent != null)
            {
                string oldPath = GetMediaItemPath(oldParent);
                string newPath = GetMediaItemPath(item);

                FileHelper.MoveFile(oldPath, newPath, item.Name, true);
                SaveMedia(item);
            }

        }

        public void Archive(IMedia item)
        {
            string path = System.IO.Path.GetDirectoryName(GetMediaItemPath(item));
            Archive(item, path);
        }

        public void Archive(IMedia item, string path)
        {
            helpers.FileHelper.ArchiveFile(path, item, true);
            
            // we actually have to clean up files too with a media delete..
            FileHelper.CleanMediaFiles(item);

            SourceInfo.Remove(item.Key);
            ImportPairs.Remove(item.Key); 

        }

    }
}
