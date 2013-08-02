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

namespace jumoo.usync.content
{
    public class ContentEvents
    {
        public ContentEvents()
        {
        }

        public void AttachEvents()
        {
            
            LogHelper.Info(typeof(ContentEvents), "Attaching to Save/Delete events");
            ContentService.Saved += ContentService_Saved;
            ContentService.Trashing += ContentService_Trashing;
            // ContentService.Moved += ContentService_Moved;        
        }

        void ContentService_Moved(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            // when something moves ...       
            LogHelper.Info<ContentWalker>("Move Event {0} {1} {2}", () => e.ParentId, ()=> e.Entity.ParentId, ()=> SourceInfo.GetParent(e.Entity.Key));
           
        }

        void ContentService_Trashing(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            LogHelper.Info(typeof(ContentEvents), String.Format("Trashed {0}", e.Entity.Name));
            ArchiveContentItem(e.Entity);
        }

        void ContentService_Trashed(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            LogHelper.Info(typeof(ContentEvents), String.Format("Trashed {0}", e.Entity.Name));
            ArchiveContentItem(e.Entity);
        }

        void ContentService_Deleted(IContentService sender, Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            LogHelper.Info(typeof(ContentEvents), "Deleted");
            ArchiveContentItems(e.DeletedEntities); 
        }

        void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            LogHelper.Info(typeof(ContentEvents), "Saved");
            SaveContentItemsToDisk(sender, e.SavedEntities); 
        }


        void SaveContentItemsToDisk(IContentService sender, IEnumerable<IContent> items)
        {
            SourceInfo.Load(); 

            ContentWalker w = new ContentWalker();
            foreach (var item in items)
            {
                if (item.Name != SourceInfo.GetName(item.Key))
                {
                    w.RenameContent(item, SourceInfo.GetName(item.Key));
                }
                
                if (item.ParentId != SourceInfo.GetParent(item.Key))
                {
                    // it's moved...
                    w.MoveContent(item, SourceInfo.GetParent(item.Key));
                }

                w.SaveContent(item); 

            }
            SourceInfo.Save(); 
        }

        void ArchiveContentItems(IEnumerable<IContent> items)
        {
            SourceInfo.Load();
            ContentWalker w = new ContentWalker();
            foreach (var item in items)
            {
                w.ArchiveContent(item);
            }
            SourceInfo.Save(); 
        }

        void ArchiveContentItem(IContent item)
        {
            SourceInfo.Load();
            ContentWalker w = new ContentWalker();
            w.ArchiveContent(item);
            SourceInfo.Save();
        }
    }
}
