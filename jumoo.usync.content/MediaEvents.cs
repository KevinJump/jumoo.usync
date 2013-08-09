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
            LogHelper.Info<ContentEvents>("attaching to media events");

            MediaService.Saved += MediaService_Saved;
            MediaService.Trashed += MediaService_Trashed;
            MediaService.Moved += MediaService_Moved;
        }

        void MediaService_Moved(IMediaService sender, Umbraco.Core.Events.MoveEventArgs<IMedia> e)
        {
            // throw new NotImplementedException();
        }

        void MediaService_Trashed(IMediaService sender, Umbraco.Core.Events.MoveEventArgs<IMedia> e)
        {
            // throw new NotImplementedException();
        }

        void MediaService_Saved(IMediaService sender, Umbraco.Core.Events.SaveEventArgs<IMedia> e)
        {
            SourceInfo.Load();

            MediaExporter me = new MediaExporter(); 
            foreach(var item in e.SavedEntities)
            {
                me.SaveMedia(item); 
            }

            SourceInfo.Save() ; 
        }
    }
}
