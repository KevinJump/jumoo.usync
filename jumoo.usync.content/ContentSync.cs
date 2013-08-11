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

                        // if the mediaItems folder isn't there we run export media
                        if (!Directory.Exists(IOHelper.MapPath(uSyncContentSettings.MediaFolder)) || uSyncContentSettings.Export)
                        {
                            ExportMedia();
                        }

                        // we do the import
                        if (uSyncContentSettings.Import)
                        {
                            ImportContent();
                            ImportMedia();
                            MapContent(); 
                        }

                        // we attach events...
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

        #region Import
        public int ImportContent()
        {
            ContentImporter ci = new ContentImporter();

            // 1. import the content 
            int importCount = ci.ImportDiskContent();

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
        #endregion

        #region Export
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
        #endregion 

        #region Events 
        public void AttachEvents()
        {
            ContentEvents events = new ContentEvents();
            events.AttachEvents(); // on the save/delete.

            MediaEvents mEvents = new MediaEvents();
            mEvents.AttachEvents();
        }
        #endregion 
    }
}
