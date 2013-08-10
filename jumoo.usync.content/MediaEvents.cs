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
    public class MediaEvents
    {
        public void AttachEvents()
        {
            LogHelper.Info<MediaEvents>("attaching to media events");

            MediaService.Saved += MediaService_Saved;
            MediaService.Trashing += MediaService_Trashing;
        }

        void MediaService_Trashing(IMediaService sender, Umbraco.Core.Events.MoveEventArgs<IMedia> e)
        {
            LogHelper.Info<MediaEvents>("Archiving {0}", () => e.Entity.Name); 
            MediaExporter me = new MediaExporter();
            me.Archive(e.Entity);
        }

        void MediaService_Saved(IMediaService sender, Umbraco.Core.Events.SaveEventArgs<IMedia> e)
        {
            SaveMediaToDisk(e.SavedEntities);
        }

        private void SaveMediaToDisk(IEnumerable<IMedia> items)
        {
            SourceInfo.Load();

            MediaExporter me = new MediaExporter();
            foreach(var item in items)
            {
                string sourceName = SourceInfo.GetName(item.Key);
                if ((sourceName != null) && (item.Name != sourceName))
                {
                    LogHelper.Info<MediaEvents>("Rename {0}", () => item.Name);
                    me.Rename(item, sourceName);
                }

                int? parent = SourceInfo.GetParent(item.Key);
                if ((parent != null) && (item.ParentId != parent.Value))
                {
                    LogHelper.Info<MediaEvents>("Move {0}", () => item.Name);
                    me.Move(item, parent.Value);
                }

                LogHelper.Info<MediaEvents>("Saving {0}", () => item.Name);
                me.SaveMedia(item); 
            }

            SourceInfo.Save(); 
        }
    }
}
