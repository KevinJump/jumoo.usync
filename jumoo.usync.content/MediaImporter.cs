using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using jumoo.usync.content.helpers;

using System.IO;

using System.Xml;
using System.Xml.Linq;

using System.Diagnostics; 

namespace jumoo.usync.content
{
    public class MediaImporter
    {
        IMediaService _mediaService;
        int _count; 

        public MediaImporter()
        {
            _mediaService = ApplicationContext.Current.Services.MediaService;

            ImportPairs.LoadFromDisk();        
        }

        public int ImportMedia()
        {
            string mediaRoot = FileHelper.uSyncMediaRoot;
            _count = 0; 

            LogHelper.Info<MediaImporter>(">> Media Import starting");
            Stopwatch sw = Stopwatch.StartNew();

            ImportMedia(mediaRoot, -1);

            ImportPairs.SaveToDisk();
            SourceInfo.Save();

            sw.Stop();
            LogHelper.Info<MediaImporter>("<< Media Import complete [{0} seconds]", () => sw.Elapsed.TotalSeconds);

            return _count; 
        }

        public void ImportMedia(string path, int parentId)
        {
            if (Directory.Exists(path))
            {
                LogHelper.Debug<MediaImporter>("ImportMedia {0}", () => path); 

                foreach (string file in Directory.GetFiles(path, "*.media"))
                {
                    LogHelper.Debug<MediaImporter>("Import Media file {0}", () => file); 
                    XElement element = XElement.Load(file);

                    if (element != null)
                    {
                        IMedia item = ImportMediaItem(element, parentId);

                        if (item != null)
                        {
                            string folderPath = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                            if (Directory.Exists(folderPath))
                            {
                                ImportMedia(folderPath, item.Id);
                            }
                        }
                    }
                }
            }
        }

        public IMedia ImportMediaItem(XElement element, int parentId)
        {
            bool _new = false;

            Guid mediaGuid = new Guid(element.Attribute("guid").Value);

            Guid _guid = ImportPairs.GetTargetGuid(mediaGuid);

            string name = element.Attribute("nodeName").Value;
            string mediaAlias = element.Attribute("mediaTypeAlias").Value ; 

            DateTime updateDate = DateTime.Now;

            if (element.Attribute("updated") != null)
            {
                updateDate = DateTime.Parse(element.Attribute("updated").Value);
            }


            IMedia media = _mediaService.GetById(_guid);
            if (media == null)
            {
                // this is new
                media = _mediaService.CreateMedia(name, parentId, mediaAlias);
                _new = true;
                LogHelper.Info<MediaImporter>("Create Media Type {0}", () => name); 

            }
            else
            {

                if (media.Trashed == true)
                {
                    // this is in the bin, create a new one
                    media = _mediaService.CreateMedia(name, parentId, mediaAlias);
                    LogHelper.Info<MediaImporter>("Item in trash, creating new {0}", ()=> name); 
                    _new = true;
                }
                else
                {                 
                 
                    // data logic to speed stuff up

                    // this doesn't seem to work, because media.UpdateDate doesn't return proper?
                    if (DateTime.Compare(updateDate, media.UpdateDate.ToLocalTime()) <= 0)
                    {
                        LogHelper.Info<MediaImporter>("No update since last change {0}", () => name);
                        return media;
                    }
                    LogHelper.Info<MediaImporter>("Updating existing media node {0}", () => name); 
                }
            }
            
            if (media != null)
            {
                _count++;
                if (media.Name != name) 
                    media.Name = name;

                if (media.ParentId != parentId)
                    media.ParentId = parentId;

                // load all the properties 
                /*
                var properties = from property in element.Elements()
                                 where property.Attribute("isDoc") == null
                                 select property;

                foreach (var property in properties)
                {
                    // LogHelper.Info(typeof(ContentImporter), String.Format("Property: {0}", property.Name)); 

                    string propertyTypeAlias = property.Name.LocalName;
                    if (media.HasProperty(propertyTypeAlias))
                    {
                        // right if we are trying to be clever and map ids 
                        // then mapIds will be set
                        media.SetValue(propertyTypeAlias, GetImportInnerXML(property));
                    }
                }
                */

                FileHelper.ImportMediaFile(mediaGuid, media);               
                
                _mediaService.Save(media); 

            }

            if (_new)
            {
                // it's new add it to the import table
                ImportPairs.SavePair(mediaGuid, media.Key);
                SourceInfo.Add(media.Key, media.Name, media.ParentId);
            }

            return media; 
        }

        private string GetImportInnerXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            string xml = reader.ReadInnerXml();

            // except umbraco then doesn't like content
            // starting cdata
            if (xml.StartsWith("<![CDATA["))
            {
                return parent.Value;
            }
            return xml;
        }
    }
}
