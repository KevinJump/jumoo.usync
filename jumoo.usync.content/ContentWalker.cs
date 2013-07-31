using System;
using System.Linq;

using Umbraco.Core ;
using Umbraco.Core.Services;
using Umbraco.Core.Models ;
using Umbraco.Core.Logging; 

using umbraco;

using System.Xml.Linq;
using System.IO;
using Umbraco.Core.IO; 


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


        public void WalkSite()
        {
            LogHelper.Info(typeof(ContentWalker), "Content Walk started"); 

            ContentService _contentService = new ContentService();

            foreach (var item in _contentService.GetRootContent())
            {
                WalkSite("", item, _contentService);
            }

            // save the pair table we use it for mapping...
            helpers.SourcePairs.SaveToDisk();

            LogHelper.Info(typeof(ContentWalker), "Content Walk Completed"); 
        }

        public void WalkSite(string path, IContent item, ContentService _contentService)
        {

            LogHelper.Info(typeof(ContentWalker), string.Format("Walking Site {0}", item.Name.ToSafeAliasWithForcingCheck()) ); 

            /*
            XElement itemXml = ExportContent(item);

            if (itemXml != null)
            {
                if (helpers.FileHelper.SaveContentFile(path, item, itemXml))
                {
                    // add to the source pair (need to write)
                    helpers.SourcePairs.SavePair(item.Id, item.Key);
                }
                
            }
            */
            SaveContent(item, path); 

            // get the child path (helper for clean path)
            path = string.Format("{0}\\{1}", path, helpers.FileHelper.CleanFileName(item.Name));

            foreach (var child in _contentService.GetChildren(item.Id))
            {
                WalkSite(path, child, _contentService);
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
                    helpers.SourcePairs.SavePair(content.Key, content.Id);
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

        public XElement ExportContent(IContent content)
        {
            LogHelper.Info(typeof(ContentWalker), String.Format("Exporting content {0}", content.Name.ToSafeAliasWithForcingCheck()));

            var nodeName = UmbracoSettings.UseLegacyXmlSchema ? "node" : content.ContentType.Alias.ToSafeAliasWithForcingCheck();

            XElement xml = new XElement(nodeName);
            
            // core content item properties 

            // if this content has come from somewhere else, i should respect that.
            // and write the orginal contentGuid into my source document. 
            Guid _guid = content.Key; 
            if (helpers.ImportPairs.pairs.ContainsValue(content.Key))
            {
                _guid = helpers.ImportPairs.pairs.FirstOrDefault(x => x.Value == content.Key).Key;
            }

            xml.Add(new XAttribute("guid", _guid));


            xml.Add(new XAttribute("parentGUID", content.Level > 1 ? content.Parent().Key : new Guid("00000000-0000-0000-0000-000000000000")));
            xml.Add(new XAttribute("nodeTypeAlias", content.ContentType.Alias));
            xml.Add(new XAttribute("templateAlias", content.Template == null ? "" : content.Template.Alias));
            xml.Add(new XAttribute("id", content.Id));
            xml.Add(new XAttribute("sortOrder", content.SortOrder));
            xml.Add(new XAttribute("nodeName", content.Name));
            xml.Add(new XAttribute("isDoc", ""));
            xml.Add(new XAttribute("published", content.Published));
            xml.Add(new XAttribute("updated", content.UpdateDate)); 
            // xml.Add(new XAttribute("releaseDate", content.ReleaseDate));
            
            // the properties of content
            foreach (var property in content.Properties.Where(p => p != null))
            {
                xml.Add(property.ToXml()); 
            }

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
