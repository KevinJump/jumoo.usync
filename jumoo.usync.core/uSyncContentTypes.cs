using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using System.Xml;
using System.Xml.Linq;

using System.IO; 

namespace jumoo.usync.core
{

    /// <summary>
    ///  syncs content types (that's documentTypeDefinitions)
    /// </summary>
    public class uSyncContentTypes : IUSyncControl
    {
        private IContentTypeService _contentTypeService;

        private Dictionary<IContentType, XElement> imported; 

        public uSyncContentTypes()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
        }

        public void Import()
        {
            imported = new Dictionary<IContentType, XElement>();
            // start at the root, and enumerate the xml...

            string docRoot = Umbraco.Core.IO.IOHelper.MapPath("~/uSync/DocumentTypes/");
            Import(docRoot); 

            // do strcuture...
                        
        }

        private void Import(string path)
        {
            // import types here...
            foreach (string file in Directory.GetFiles("*.config"))
            {
                XElement e = XElement.Load(file);
                if (e != null)
                {
                    IEnumerable<IContentType> imports = ApplicationContext.Current.Services.PackagingService.ImportContentTypes(e, false);

                    foreach (var item in imports)
                    {
                        imported.Add(item, e); 
                        Import(Path.Combine(path, item.Alias));
                    }
                }
            }
        }

        private void BuildStructure()
        {
            foreach (KeyValuePair<IContentType, XElement> import in imported)
            {
                // do the structure for this ContentType 
                ImportStructure(import.Key, import.Value); 
            }
        }

        private void ImportStructure(IContentType item, XElement node)
        {
            XElement structure = node.Element("Structure");

            List<ContentTypeSort> allowed = new List<ContentTypeSort>();
            int sortOrder = 0;

            foreach (var doc in structure.Elements("DocumentType"))
            {
                string alias = doc.Value;
                IContentType aliasDoc = ApplicationContext.Current.Services.ContentTypeService.GetContentType(alias);

                if (aliasDoc != null)
                {
                    allowed.Add(new ContentTypeSort(new Lazy<int>(() => aliasDoc.Id), sortOrder, aliasDoc.Name));
                    sortOrder++;
                }
            }

            item.AllowedContentTypes = allowed;
        }

        public void Export()
        {
            LogHelper.Info<uSyncContentTypes>("Exporting Content Types"); 
            foreach (var item in _contentTypeService.GetAllContentTypes())
            {
                SaveDocType(item);                                          
            }
            
        }

        public void Attach()
        {
            // attach to the events...
            ContentTypeService.SavedContentType +=ContentTypeService_SavedContentType;
            ContentTypeService.DeletedContentType += ContentTypeService_DeletedContentType;
            LogHelper.Debug<uSyncContentTypes>("Attached to Events"); 
        }

        #region Events

        void ContentTypeService_DeletedContentType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<Umbraco.Core.Models.IContentType> e)
        {
            foreach (var item in e.DeletedEntities)
            {
                DeleteDocType(item);
            }
        }

        void ContentTypeService_SavedContentType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<Umbraco.Core.Models.IContentType> e)
        {
            foreach (var item in e.SavedEntities)
            {
                SaveDocType(item);
            }
        }

        #endregion 

        #region manipulation 

        private void SaveDocType(IContentType item)
        {
            // get the path...
            string folderPath = GetDocTypePath(item) ;

            // do the save... 
            // the packaging service does ContentType Exports..
            XElement element = ApplicationContext.Current.Services.PackagingService.Export(item);
            string filePath = Path.Combine(folderPath, "def.config");

            LogHelper.Info<uSyncContentTypes>("Saving {0} at {1}", () => item.Name, () => filePath);
        
            Helpers.uSyncIO.SaveXmlFile(element, filePath);
        }

        private void DeleteDocType(IContentType item)
        {
            string filePath = Path.Combine(GetDocTypePath(item), "def.config"); 

            // archive
            Helpers.uSyncIO.ArchiveFile(filePath); 
        }

        private string GetDocTypePath(IContentType item)
        {
            return GetDocTypePath(item, "DocumentTypes");
        }

        private string GetDocTypePath(IContentType item, string path)
        {
            if (item.ParentId != -1)
            {
                IContentType parent = _contentTypeService.GetContentType(item.ParentId);
                if (parent != null)
                {
                    path = GetDocTypePath(parent, path);
                }
            }

            path = String.Format("{0}\\{1}", path, item.Alias); 

            return path ;
        }

        #endregion 

    }
}
