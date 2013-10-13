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
    public class ContentExporter
    {
        int _count;
        IContentService _contentService; 

        public ContentExporter()
        {
            _contentService = ApplicationContext.Current.Services.ContentService; 
        }


        public int WalkSite(bool pairs)
        {
            LogHelper.Info<ContentExporter>("Content Walk Started");
            Stopwatch sw = Stopwatch.StartNew();
            _count = 0;

            if (!String.IsNullOrEmpty(uSyncContentSettings.RootId))
            {
                LogHelper.Info<ContentExporter>("Starting content walk from ID = {0}", () => uSyncContentSettings.RootId);
                // walk from another place that isn't the root.
                IContent _rootContent = _contentService.GetById(int.Parse(uSyncContentSettings.RootId));

                if (_rootContent != null)
                {
                    string contentPath = GetContentPath(_rootContent);
                    WalkSite(contentPath, _rootContent, pairs);
                }
            }
            else
            {
                foreach (var item in _contentService.GetRootContent())
                {
                    WalkSite("", item, pairs);
                }
            }

            // save the pair table we use it for renames...
            SourceInfo.Save();

            sw.Stop(); 
            LogHelper.Info<ContentExporter>("Content Walk Completed [{0} milliseconds]", ()=> sw.Elapsed.TotalMilliseconds );
            return _count; 
        }

        public void WalkSite(string path, IContent item, bool pairs)
        {
            if (pairs)
            {
                SourceInfo.Add(item.Key, item.Name, item.ParentId);
            }
            else
            {
                LogHelper.Info<ContentExporter>("Walking Site {0}", () => FileHelper.CleanFileName(item.Name));
                SaveContent(item, path);
            }
            
            path = string.Format("{0}\\{1}", path, FileHelper.CleanFileName(item.Name));
            foreach (var child in _contentService.GetChildren(item.Id))
            {
                WalkSite(path, child, pairs);
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
                    _count++;
                    SourceInfo.Add(content.Key, content.Name, content.ParentId);                    
                }
            }
            else {
                LogHelper.Info(typeof(ContentExporter), "Content XML came back as null");
            }

        }

        public void ArchiveContent(IContent content)
        {
            string path = Path.GetDirectoryName(GetContentPath(content));
            ArchiveContent(content, path);
        }

        public void ArchiveContent(IContent content, string path)
        {
            helpers.FileHelper.ArchiveFile(path, content);
            SourceInfo.Remove(content.Key);
            ImportPairs.Remove(content.Key); 
        }

        public void RenameContent(IContent content, string oldName)
        {
            string path = Path.GetDirectoryName(GetContentPath(content));
            helpers.FileHelper.RenameFile(path, content, oldName); 
        }

        public void MoveContent(IContent content, int oldParentId)
        {
            ContentService contentService = new ContentService() ;

            IContent oldParent = contentService.GetById(oldParentId);

            if (oldParent != null)
            {
                string oldPath = GetContentPath(oldParent);
                string newPath = GetContentPath(content);

                FileHelper.MoveFile(oldPath, newPath, content.Name);
                SaveContent(content); 
            }
        }


        public XElement ExportContent(IContent content)
        {
            LogHelper.Info(typeof(ContentExporter), String.Format("Exporting content {0}", FileHelper.CleanFileName(content.Name)));

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
    }
}
