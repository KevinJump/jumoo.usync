using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging; 

namespace jumoo.usync.content
{
    public class ContentSync : IApplicationEventHandler
    {
        private static bool _synced = false;
        private static object _oSync = new object(); 

        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (!_synced)
            {
                lock (_oSync)
                {
                    if (!_synced)
                    {
                        // do first time stuff...
                        LogHelper.Info(typeof(ContentSync), "Initalized uSync Content Edition"); 
                    }
                }
            }
        }

        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // not us
        }
        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // not us
        }
    }
}
