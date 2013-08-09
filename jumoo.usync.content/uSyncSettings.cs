using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration; 

using Umbraco.Core.IO ; 
using Umbraco.Core.Logging ; 


namespace jumoo.usync.content
{
    public class uSyncContentSettingsSection : ConfigurationSection 
    {
        /// <summary>
        /// export content at startup
        /// </summary>
        [ConfigurationProperty("export", DefaultValue = true, IsRequired = false)]
        public Boolean Export
        {
            get { return (Boolean)this["export"]; }
        }
        
        /// <summary>
        /// import the content at startup
        /// </summary>
        [ConfigurationProperty("import", DefaultValue = true, IsRequired = false)]
        public Boolean Import
        {
            get { return (Boolean)this["import"]; }
        }

        /// <summary>
        /// attach to the events, save/delete etc and write when they happen
        /// 
        /// accepted values, are "save" and "publish", 
        /// with save, files will be written everysave
        /// with publish they are written when something is published
        /// </summary>
        [ConfigurationProperty("events", DefaultValue = true, IsRequired = false)]
        public Boolean Events
        {
            get { return (Boolean)this["events"]; }
        }

        [ConfigurationProperty("folder", DefaultValue = "~/uSync/Content/", IsRequired = false)]
        public String Folder
        {
            get { return (String)this["folder"]; }
        }

        [ConfigurationProperty("media", DefaultValue = "~/uSync/Media/", IsRequired = false)]
        public String Media {
            get { return (String)this["media"] ; }
        }

        [ConfigurationProperty("files", DefaultValue= "~/uSync/Files/", IsRequired = false)]
        public String Files {
            get { return (String)this["files"] ; }
        }

        [ConfigurationProperty("versions", DefaultValue = true, IsRequired = false)]
        public Boolean Versions
        {
            get { return (Boolean)this["versions"]; }
        }

        [ConfigurationProperty("archive", DefaultValue = "~/uSync.Archive/Content/", IsRequired = false)]
        public String Archive
        {
            get { return (String)this["archive"]; }
        }
    }

    public class uSyncContentSettings
    {
        private static string _settingsFile = "usyncContent.Config";
        private static uSyncContentSettingsSection _settings ; 
        
        static uSyncContentSettings()
        {
            try
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = IOHelper.MapPath(String.Format("~/config/{0}", _settingsFile));

                // load the settings
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                _settings = (uSyncContentSettingsSection)config.GetSection("usync.content");
            }
            catch (Exception ex)
            {
                LogHelper.Error(typeof(uSyncContentSettings), "error loading settings file", ex);
            }
        }

        public static bool Export
        {
            get { return _settings.Export; }
        }

        public static bool Import
        {
            get { return _settings.Import; }
        }

        public static bool Events
        {
            get { return _settings.Events; }
        }

        public static string Folder
        {
            get { return _settings.Folder; }
        }

        public static string ArchiveFolder
        {
            get { return _settings.Archive; }
        }

        public static bool Versions
        {
            get { return _settings.Versions; }
        }

        public static string MediaFolder {
            get { return _settings.Media ; }
        }

        public static string Files { 
            get { return _settings.Files; }
        }



    }   
}
