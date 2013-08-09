using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging;

using Umbraco.Core.IO;
using System.IO;

using System.Diagnostics;

namespace jumoo.usync.content
{
    public class ContentSync : ApplicationEventHandler
    {
        private static bool _synced = false;
        private static object _oSync = new object();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (!_synced)
            {
                lock (_oSync)
                {
                    if (!_synced)
                    {
                        // do first time stuff...
                        LogHelper.Info(typeof(ContentSync), "Initalizing uSync Content Edition");
                        Stopwatch sw = Stopwatch.StartNew(); 

                        if (!Directory.Exists(IOHelper.MapPath(uSyncContentSettings.Folder)) || uSyncContentSettings.Export)
                        {
                            ExportContent(false); 
                        }
                        else {
                            // if the source info file is missing 
                            if (helpers.SourceInfo.IsNew())
                            {
                                LogHelper.Info<ContentSync>("Creating Source xml"); 
                                ExportContent(true);
                            }
                                
                        }

                        if (!Directory.Exists(IOHelper.MapPath(uSyncContentSettings.MediaFolder)) || uSyncContentSettings.Export)
                        {
                            ExportMedia();
                        }

                        if (uSyncContentSettings.Import)
                        {
                            ImportContent();
                            ImportMedia();
                            MapContent(); 
                        }

                        if (uSyncContentSettings.Events)
                        {
                            AttachEvents();
                        }

                        sw.Stop(); 
                        LogHelper.Info<ContentSync>("uSync Content Edition Initilized [{0} seconds]", () => sw.Elapsed.TotalSeconds); 
                    }
                }
            }
        }

        public int ImportContent()
        {
            ContentImporter ci = new ContentImporter();

            // 1. import the content 
            int importCount = ci.ImportDiskContent();
/*
            // 2. to map id's in the things that we just updated. 
            ci.MapContentIds();
 */

            return importCount; 
        }

        public void MapContent()
        {
            ContentImporter ci = new ContentImporter();
            ci.MapContentIds();
        }

        public void ImportMedia()
        {
            MediaImporter mi = new MediaImporter();
            mi.ImportMedia();
        }

        public void ExportContent(bool pairs)
        {
            // do the content exoort
            ContentWalker cw = new ContentWalker();
            cw.WalkSite(pairs);    
        }

        public void ExportMedia()
        {
            MediaExporter me = new MediaExporter();
            me.Export();
        }

        public void AttachEvents()
        {
            ContentEvents events = new ContentEvents();
            events.AttachEvents(); // on the save/delete.

            MediaEvents mEvents = new MediaEvents();
            mEvents.AttachEvents(); 
        }
    }
}
