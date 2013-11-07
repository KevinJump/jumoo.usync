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

        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            jumps.umbraco.usync.uSync.Initialized += uSync_Initialized;
        }

        /// <summary>
        /// fired when uSync has finished...
        /// </summary>
        /// <param name="e"></param>
        void uSync_Initialized(jumps.umbraco.usync.uSyncEventArgs e)
        {
            DoStartup();
        }
        
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // DoStartup(); 
        }

        private void DoStartup() 
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

        /// <summary>
        ///  import everything from Disk 
        ///     we import content, then media then we attempt to map any ids (in content)
        /// </summary>
        public int ImportAll()
        {
            int count = 0;
            count = ImportContent();
            count += ImportMedia();

            MapContent();

            return count; 
        }


        public int ImportContent()
        {
            ContentImporter ci = new ContentImporter();
            return ci.ImportDiskContent();
        }

        public void MapContent()
        {
            ContentImporter ci = new ContentImporter();
            ci.MapContentIds();
        }

        public int ImportMedia()
        {
            MediaImporter mi = new MediaImporter();
            return mi.ImportMedia();
        }
        #endregion

        #region Export
        public int ExportContent(bool pairs)
        {
            // do the content exoort
            ContentExporter cw = new ContentExporter();
            return cw.ExportSite(pairs);    
        }

        public int ExportMedia()
        {
            MediaExporter me = new MediaExporter();
            return me.Export();
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
