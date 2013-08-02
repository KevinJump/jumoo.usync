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

        public void AttachEvents(bool onPublish)
        {
            // two types of sync, 
            //
            // 1. only writes out published things,
            // 2. does everything you save.
            //

  /*          if (onPublish)
            {
                // if we are doing on publish sync...
                LogHelper.Info(typeof(ContentEvents), "Attaching to publish/unpublish events");
                
                ContentService.Published += ContentService_Published;
                ContentService.UnPublished += ContentService_UnPublished;
            }
            else 
   */
            {
                // if we are doing on save sync 
                LogHelper.Info(typeof(ContentEvents), "Attaching to Save/Delete events");
                ContentService.Saved += ContentService_Saved;
                // ContentService.Deleted += ContentService_Deleted; (not deleted, we want it in the bin)
                // ContentService.Trashed += ContentService_Trashed;
                ContentService.Trashing += ContentService_Trashing;
                ContentService.Moved += ContentService_Moved;
            }

        }

        void ContentService_Moved(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            // thing has moved... we should tidy up... but we need to know what it was called
            LogHelper.Info<ContentEvents>("Move Fired Name: {0} Parent: {1}", () => e.Entity.Name, () => e.Entity.ParentId);

            foreach (var version in sender.GetVersions(e.Entity.Id))
            {
                LogHelper.Info<ContentEvents>("Old Version Name: {0} Parent: {1}", () => version.Name, () => version.ParentId);
            }
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
/*
        void ContentService_UnPublished(Umbraco.Core.Publishing.IPublishingStrategy sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            LogHelper.Info(typeof(ContentEvents), "UnPublished");
            ArchiveContentItems(e.PublishedEntities); 
        }

        void ContentService_Published(Umbraco.Core.Publishing.IPublishingStrategy sender, Umbraco.Core.Events.PublishEventArgs<IContent> e)
        {
            // when something is published, save it to the tree...
            LogHelper.Info(typeof(ContentEvents), "Published");
            SaveContentItemsToDisk(sender, e.PublishedEntities); 
        }
*/

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


                // rename (bascially delete the file (we've just saved a new one - rename the folder?)
                // RenameContent(last, item.Name); 
                
                

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
