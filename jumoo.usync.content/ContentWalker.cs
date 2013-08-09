using System;
using System.Linq;

using System.Collections.Generic;
using System.Text.RegularExpressions;

using Umbraco.Core ;
using Umbraco.Core.Services;
using Umbraco.Core.Models ;
using Umbraco.Core.Logging; 

using umbraco;

using System.Xml;
using System.Xml.Linq;

using System.IO;
using Umbraco.Core.IO;

using System.Diagnostics;

using jumoo.usync.content.helpers; 


namespace jumoo.usync.content
{
    /// <summary>
    ///  content walker, walks a content tree and writes it out to disk
    /// </summary>
    public class ContentWalker
    {
        public ContentWalker()
        {

        }


        public void WalkSite(bool pairs)
        {
            LogHelper.Info<ContentWalker>("Content Walk Started");
            Stopwatch sw = Stopwatch.StartNew(); 

            ContentService _contentService = new ContentService();

            foreach (var item in _contentService.GetRootContent())
            {
                WalkSite("", item, _contentService, pairs);
            }

            // save the pair table we use it for renames...
            SourceInfo.Save();

            sw.Stop(); 
            LogHelper.Info<ContentWalker>("Content Walk Completed [{0} milliseconds]", ()=> sw.Elapsed.TotalMilliseconds ); 
        }

        public void WalkSite(string path, IContent item, ContentService _contentService, bool pairs)
        {
            if (pairs)
            {
                SourceInfo.Add(item.Key, item.Name, item.ParentId);
            }
            else
            {
                LogHelper.Info<ContentWalker>("Walking Site {0}", () => FileHelper.CleanFileName(item.Name));
                SaveContent(item, path);
            }
            
            path = string.Format("{0}\\{1}", path, FileHelper.CleanFileName(item.Name));
            foreach (var child in _contentService.GetChildren(item.Id))
            {
                WalkSite(path, child, _contentService, pairs);
            }

        }

        public void SaveContent(IContent content)
        {
            string path = Path.GetDirectoryName(GetContentPath(content));
            SaveContent(content, path);
        }
        
        public void SaveContent(IContent content, string path)
        {
            // given a bit of content, save it in the right place...
            XElement itemXml = ExportContent(content);

            if (itemXml != null)
            {
                if (helpers.FileHelper.SaveContentFile(path, content, itemXml))
                {
                    SourceInfo.Add(content.Key, content.Name, content.ParentId);                    
                }
            }
            else {
                LogHelper.Info(typeof(ContentWalker), "Content XML came back as null");
            }

        }

        public void ArchiveContent(IContent content)
        {
            string path = Path.GetDirectoryName(GetContentPath(content));
            ArchiveContent(content, path);
        }

        public void ArchiveContent(IContent content, string path)
        {
            helpers.FileHelper.ArchiveContentFile(path, content); 
        }

        public void RenameContent(IContent content, string oldName)
        {
            string path = Path.GetDirectoryName(GetContentPath(content));
            helpers.FileHelper.RenameContentFile(path, content, oldName); 
        }

        public void MoveContent(IContent content, int oldParentId)
        {
            ContentService contentService = new ContentService() ;

            IContent oldParent = contentService.GetById(oldParentId);

            if (oldParent != null)
            {
                string oldPath = GetContentPath(oldParent);
                string newPath = GetContentPath(content);

                FileHelper.MoveContentFile(oldPath, newPath, content.Name);
                SaveContent(content); 
            }
        }


        public XElement ExportContent(IContent content)
        {
            LogHelper.Info(typeof(ContentWalker), String.Format("Exporting content {0}", FileHelper.CleanFileName(content.Name)));

            var nodeName = UmbracoSettings.UseLegacyXmlSchema ? "node" : FileHelper.CleanFileName(content.ContentType.Alias);

            // XElement xml = new XElement(nodeName);

            XElement xml = uSyncXmlHelper.ExportContentBase(nodeName, content); 
                        
            // core content item properties 
            xml.Add(new XAttribute("parentGUID", content.Level > 1 ? content.Parent().Key : new Guid("00000000-0000-0000-0000-000000000000")));
            xml.Add(new XAttribute("nodeTypeAlias", content.ContentType.Alias));
            xml.Add(new XAttribute("templateAlias", content.Template == null ? "" : content.Template.Alias));

            xml.Add(new XAttribute("sortOrder", content.SortOrder));
            xml.Add(new XAttribute("published", content.Published));

            return xml; 
        }

        private string GetContentPath(IContent content)
        {
            // works out the path for our content
            string path = "";

            path = helpers.FileHelper.CleanFileName(content.Name);

            if (content.ParentId != -1)
            {
                path = String.Format("{0}\\{1}", GetContentPath(content.Parent()), path);
            }
            
            return path; 
        }

        /*
        private string ReplaceIdsWithGuid(string propValue)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            // look for things that might be Ids
            foreach (Match m in Regex.Matches(propValue, @"\d{1,10}"))
            {
                Guid? localGuid = GetGuidFromId(int.Parse(m.Value));
                if ( localGuid != null ) 
                {
                    if (!replacements.ContainsKey(m.Value))
                    {
                        Guid sourceGuid = helpers.ImportPairs.GetSourceGuid(localGuid.Value); 
                        replacements.Add(m.Value, sourceGuid.ToString().ToUpper());
                    }
                }
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                LogHelper.Info(typeof(ContentWalker), String.Format("Updating Id's {0} > {1}", pair.Key, pair.Value));
                propValue = propValue.Replace(pair.Key, pair.Value);
            }

            LogHelper.Debug(typeof(ContentWalker), String.Format("Updated [{0}]", propValue));
            return propValue; 
        }

        private Guid? GetGuidFromId(int id)
        {
            ContentService cs = new ContentService();

            IContent contentItem = cs.GetById(id);
            if (contentItem != null)
                return contentItem.Key;
            else
                return null;

        }

        private string GetInnerXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }
         */
    }


}
