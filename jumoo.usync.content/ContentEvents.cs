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
            ContentService.Copied += ContentService_Copied;
        }

        void ContentService_Copied(IContentService sender, Umbraco.Core.Events.CopyEventArgs<IContent> e)
        {
            SaveContentItemsToDisk(sender, new List<IContent>(new IContent[] { e.Copy }));
        }

        /// <summary>
        ///  when something is deleted it's plopped in the recycle bin
        /// </summary>
        void ContentService_Trashing(IContentService sender, Umbraco.Core.Events.MoveEventArgs<IContent> e)
        {
            LogHelper.Info<ContentEvents>("Trashing {0}", () => e.Entity.Name);
            ArchiveContentItem(e.Entity);



        }

        /// <summary>
        ///  saved is fired on save and when something is moved (save fires before the move event)
        /// </summary>
        void ContentService_Saved(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            SaveContentItemsToDisk(sender, e.SavedEntities); 
        }

        /// <summary>
        ///  our save routine, checks to see if the item has been renamed, or if it's been moved
        ///  then saves. 
        /// </summary>
        void SaveContentItemsToDisk(IContentService sender, IEnumerable<IContent> items)
        {
            SourceInfo.Load(); 

            ContentExporter w = new ContentExporter();
            foreach (var item in items)
            {
                LogHelper.Info<ContentExporter>("Saving {0} [{1}]", () => item.Name, () => item.Name.ToSafeAlias());
                string sourceName = SourceInfo.GetName(item.Key);
                if ( (sourceName!= null) && (item.Name != sourceName ) )
                {
                    LogHelper.Info<ContentExporter>("Rename {0}", () => item.Name);
                    w.RenameContent(item, SourceInfo.GetName(item.Key));
                }
                
                int? parent = SourceInfo.GetParent(item.Key) ; 
                if ( (parent != null) && (item.ParentId != parent.Value))
                {
                    LogHelper.Info<ContentExporter>("Move {0}", () => item.Name);
                    w.MoveContent(item, parent.Value);
                }

                w.SaveContent(item); 

            }
            SourceInfo.Save(); 
        }

        void ArchiveContentItems(IEnumerable<IContent> items)
        {
            SourceInfo.Load();
            ContentExporter w = new ContentExporter();
            foreach (var item in items)
            {
                w.ArchiveContent(item);
            }
            SourceInfo.Save(); 
        }

        void ArchiveContentItem(IContent item)
        {
            SourceInfo.Load();
            ContentExporter w = new ContentExporter();
            w.ArchiveContent(item);
            SourceInfo.Save();
        }
    }
}
