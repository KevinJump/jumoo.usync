using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging; 

//
// the core uSync - syncs all the basic stuff of umbraco.
//
// v2.0 -   built to work with v6.1.x and above. 
//          built to work with ContentEdition
//          this is the unified uSync.
//

namespace jumoo.usync.core
{
    /// <summary>
    ///  uSyncer - the control node for uSync.
    /// </summary>
    public class uSyncer : ApplicationEventHandler
    {
        private bool _synced = false;
        private object _oSync = new object();

        /// <summary>
        ///  called when the site starts. 
        /// </summary>
        /// <param name="umbracoApplication">app</param>
        /// <param name="applicationContext">context</param>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (!_synced)
            {
                lock (_oSync)
                {
                    if (!_synced)
                    {
                        LogHelper.Info<uSyncer>("uSyncer started..."); 
                        
                        _synced = true;

                        uSyncController controller = new uSyncController();

                        // these will be settings controlled 
                        // controller.ImportFromDisk();
                        controller.ExportToDisk();
                        controller.AttachEvents();

                        LogHelper.Info<uSyncer>("uSyncer Completed..."); 

                    }
                }
            }
            base.ApplicationStarted(umbracoApplication, applicationContext);
        }
    }

    /// <summary>
    ///  uSync Controller, controls the bits of uSync
    /// </summary>    
    public class uSyncController
    {
        public void ImportFromDisk()
        {

        }

        public void ExportToDisk()
        {
            uSyncContentTypes t = new uSyncContentTypes();
            t.Export();
        }

        public void AttachEvents()
        {
            uSyncContentTypes t = new uSyncContentTypes();
            t.Attach(); 
        }            
    }
}
