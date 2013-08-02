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
            ContentService.Moved += ContentService_Moved;        
        }

        void ContentService_Moved(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            // when something moves ...         
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
            helpers.SourcePairs.LoadFromDisk();
            ContentWalker w = new ContentWalker();
            foreach (var item in items)
            {
                if (item.Name != SourcePairs.GetName(item.Key))
                {
                    // rename 
                    w.RenameContent(item, SourcePairs.GetName(item.Key));
                }
                w.SaveContent(item); 

            }
            SourcePairs.SaveToDisk();
        }

        void ArchiveContentItems(IEnumerable<IContent> items)
        {
            // something soon
            helpers.SourcePairs.LoadFromDisk();
            ContentWalker w = new ContentWalker();
            foreach (var item in items)
            {
                w.ArchiveContent(item);
            }
            helpers.SourcePairs.SaveToDisk();
        }

        void ArchiveContentItem(IContent item)
        {
            helpers.SourcePairs.LoadFromDisk();
            ContentWalker w = new ContentWalker();
            w.ArchiveContent(item);
            helpers.SourcePairs.SaveToDisk();
        }
    }
}
