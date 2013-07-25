using System;
using System.Collections.Generic;
using System.Linq;

using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models; 
using Umbraco.Core.Logging;

using System.IO;

using System.Text.RegularExpressions; 

namespace jumoo.usync.content
{
    public class ContentImporter
    {
        PackagingService _packager;
        IContentService _contentService;

        Dictionary<int, int> _idMap = new Dictionary<int, int>();

        int importCount = 0; // used just to say how many things we imported

        public ContentImporter()
        {
            _packager = ApplicationContext.Current.Services.PackagingService;
            _contentService = ApplicationContext.Current.Services.ContentService;

            // load the import table (from disk)
            helpers.ImportPairs.LoadFromDisk(); 
        }

        public int ImportDiskContent(bool mapIds)
        {
            importCount = 0;

            string root = helpers.FileHelper.uSyncRoot;

            LogHelper.Info(typeof(ContentImporter), String.Format("Importing disk contents - IdMapping : {0}", mapIds));

            ImportDiskContent(root, -1, mapIds);

            // save the import pair table.
            // SaveImportPairTable();
            helpers.ImportPairs.SaveToDisk(); 

            return importCount; 
        }

        /// <summary>
        ///  walks the disk folder, imports any .content files it finds
        ///  in a folder, then recurses into sub folders to do the same
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parentId"></param>
        /// <param name="mapIds"></param>
        public void ImportDiskContent(string path, int parentId, bool mapIds)
        {
            LogHelper.Info(typeof(ContentImporter), String.Format("Importing Disk Content {0}", path)); 

            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path, "*.content"))
                {
                    LogHelper.Info(typeof(ContentImporter), String.Format("Found Content File {0}", file)); 

                    XElement element = XElement.Load(file);

                    if (element != null)
                    {
                        IContent item = ImportContentItem(element, parentId, mapIds);

                        if (item != null)
                        {
                            string folderPath = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                            if (Directory.Exists(folderPath))
                            {
                                ImportDiskContent(folderPath, item.Id, mapIds);
                            }
                        }
                    }
                }
            }
        }

        public IContent ImportContentItem(XElement element, int parentId, bool mapIds)
        {
            LogHelper.Info(typeof(ContentImporter), String.Format("Importing Content Item {0}", element.Name)); 

            // 
            // get the guid we ar going to use for the content.
            //
            Guid contentGuid = new Guid(element.Attribute("guid").Value);
            Guid _guid = contentGuid ;

            bool _new = false; // flag to track if we created new content.

            //
            // we look in the import table, if we have already imported this
            // guid then we should have a new guid from it. so we and use that
            // to find our content and create that. 
            // 


            if ( helpers.ImportPairs.pairs.ContainsKey(contentGuid))
            {
                _guid = helpers.ImportPairs.pairs[contentGuid];
            }

            // load all the values from the xml 
            string name = element.Attribute("nodeName").Value;
            string nodeType = element.Attribute("nodeTypeAlias").Value;
            string templateAlias = element.Attribute("templateAlias").Value;

            int sortOrder = int.Parse(element.Attribute("sortOrder").Value);
            bool published = bool.Parse(element.Attribute("published").Value);

            // try to load the content. 
            // even if we haven't imported it before, we might be
            // reimporting to a source system so we should have a go
            IContent content = _contentService.GetById(_guid);
            if (content == null)
            {
                // this is new..
                content = _contentService.CreateContentWithIdentity(name, parentId, nodeType);
                LogHelper.Info(typeof(ContentImporter), "Created New Content Node");
                _new = true; 
            }
            else
            {
                // log..
                LogHelper.Info(typeof(ContentImporter), "Found Existing Node");

            }

            if (content != null)
            {
                // now we have content
                ITemplate template = ApplicationContext.Current.Services.FileService.GetTemplate(templateAlias);

                if (template != null)
                    content.Template = template;

                content.SortOrder = sortOrder; 

                // load all the properties 
                var properties = from property in element.Elements()
                                 where property.Attribute("isDoc") == null
                                 select property;

                foreach (var property in properties)
                {
                    LogHelper.Info(typeof(ContentImporter), String.Format("Property: {0}", property.Name)); 

                    string propertyTypeAlias = property.Name.LocalName;
                    if (content.HasProperty(propertyTypeAlias))
                    {
                        // right if we are trying to be clever and map ids 
                        // then mapIds will be set
                        if (mapIds)
                        {
                            // we can only really do this once all the content is imported.
                            // so it's a bit of a two pass thing.
                            content.SetValue(propertyTypeAlias, UpdateMatchingIds(GetInnerXML(property)));

                        }
                        else
                        {
                            // just map the values
                            content.SetValue(propertyTypeAlias, GetInnerXML(property));
                        }
                    }
                }

                // do we publish?
                if (published)
                {
                    _contentService.SaveAndPublish(content, 0, false);
                }
                else
                {
                    _contentService.Save(content, 0, false);
                    // if it was already published we should unpublish here..
                    if (content.Published)
                    {
                        _contentService.UnPublish(content);
                    }
                }

                ++importCount; // for status updates...

                if (_new)
                {
                    // it's new add it to the import table
                    helpers.ImportPairs.SavePair(contentGuid, content.Key);
                    
                }

                // add the content to the id map (for second pass mapping)
                int _sourceId = int.Parse(element.Attribute("id").Value);
                if (!_idMap.ContainsKey(_sourceId))
                {
                    _idMap.Add(_sourceId, content.Id);
                }

                //
                // the reality is the GUID is no special thing here. 
                // as all new content gets a new guid, you could just 
                // as well use the ID. you can't force the guid on content
                // so their is no value, we end up mapping oldguid -> new
                // and old id -> new - when one would do, and as umbraco
                // uses id everywhere, you could just map that. 
                // 

                return content; 

            }

            return null; 

        }

        /// <summary>
        ///  takes the content, uses the IdMap to search and replace
        ///  and id's it finds in the code.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private string UpdateMatchingIds(string content)
        {
            LogHelper.Debug(typeof(ContentImporter), String.Format("Original [{0}]", content)); 

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            
            // look for things that might be Ids
            foreach(Match m in Regex.Matches(content, @"\d{1,10}"))
            {
                if ( _idMap.ContainsKey(int.Parse(m.Value)))
                {
                    // we have an id that is in our mapper

                    // it it's not already in our replacements for this 
                    // bit of content ... add it

                    int source = int.Parse(m.Value);

                    if ( !replacements.ContainsKey(m.Value))
                    {
                        replacements.Add(m.Value, _idMap[source].ToString());
                    }
                }
            }

            // now loop through our replacements and add them

            foreach(KeyValuePair<string, string> pair in replacements)
            {
                LogHelper.Info(typeof(ContentImporter), String.Format("Updating Id's {0} > {1}", pair.Key, pair.Value)); 
                content = content.Replace(pair.Key, pair.Value);
            }

            LogHelper.Debug(typeof(ContentImporter), String.Format("Updated [{0}]", content));
            return content; 
        }


        // some variables have inside xml, so we need to get to that
        // and not let the parser take it out
        private string GetInnerXML(XElement parent)
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
