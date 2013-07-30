using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;

using Umbraco.Core.Logging;

namespace jumoo.usync.content
{
    public class ContentEvents
    {
        public ContentEvents()
        {
        }

        public void AttachEvents(bool onPublish)
        {
            // two types of sync, 
            //
            // 1. only writes out published things,
            // 2. does everything you save.
            //

            if (onPublish)
            {
                // if we are doing on publish sync...
                LogHelper.Info(typeof(ContentEvents), "Attaching to publish/unpublish events");
                
                ContentService.Published += ContentService_Published;
                ContentService.UnPublished += ContentService_UnPublished;
            }
            else
            {
                // if we are doing on save sync 
                LogHelper.Info(typeof(ContentEvents), "Attaching to Save/Delete events");
                ContentService.Saved += ContentService_Saved;
                ContentService.Deleted += ContentService_Deleted;
            }

        }

        void ContentService_Deleted(IContentService sender, Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            ArchiveContentItems(e.DeletedEntities); 
        }

        void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            SaveContentItemsToDisk(e.SavedEntities); 
        }

        void ContentService_UnPublished(Umbraco.Core.Publishing.IPublishingStrategy sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            ArchiveContentItems(e.PublishedEntities); 
        }

        void ContentService_Published(Umbraco.Core.Publishing.IPublishingStrategy sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            // when something is published, save it to the tree...
            SaveContentItemsToDisk(e.PublishedEntities); 
        }

        void SaveContentItemsToDisk(IEnumerable<IContent> items)
        {
            ContentWalker w = new ContentWalker();
            foreach (var item in items)
            {
                w.SaveContent(item); 
            }
        }

        void ArchiveContentItems(IEnumerable<IContent> items)
        {
            // something soon
        }
    }
}
