/*
 * Code that doesn't work :
 *  
 * was trying to do multiple package installs, but after reboot due to dlls the 
 * installer functions assume you have full web context... you don't :( 
 * 
 * it's only one line too.. 
 * 
 * 
 */ 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco;
using Umbraco.Core;
using Umbraco.Core.Services; 
using Umbraco.Core.Logging ;
using Umbraco.Core.IO; 

using umbraco.cms.businesslogic.packager ;
using umbraco.cms.businesslogic.packager.repositories;
using BizLogicAction = umbraco.BusinessLogic.Actions.Action;

using System.Xml;
using System.Xml.Linq;

using System.IO; 


namespace jumoo.usync.packages
{
    public class uPackageSyner : ApplicationEventHandler
    {
        static bool _synced = false;
        static object _sync = new object();

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (!_synced)
            {
                lock (_sync)
                {
                    if (!_synced)
                    {
                        _synced = true;
                        PackageSync ps = new PackageSync();
                        ps.InstallPackages(); 
                    }
                }
            }
        }
    }


    public class PackageSync
    {
        /// <summary>
        ///  reads installed packages, writes it out to disk...
        /// </summary>
        public void ReadInstalledPackages()
        {
            LogHelper.Info(typeof(PackageSync), "Reading Installed Packages"); 
            foreach (var p in InstalledPackage.GetAllInstalledPackages())
            {
                LogHelper.Info(typeof(PackageSync), string.Format("Package {0} {1} {2}", p.Data.Name, p.Data.PackageGuid, p.Data.RepositoryGuid));
                uSyncPackageInfo package = new uSyncPackageInfo(p.Data.Name, p.Data.PackageGuid, p.Data.RepositoryGuid);
                package.Save(); 
            }
        }

        /// <summary>
        ///  reads packages from the uSync/Packages folder and installs them.
        ///  -- will probibly restart the installation a few times
        ///  </summary>
        public void InstallPackages()
        {
            ///
            // 1. load packages from disk...
            //
            string path = IOHelper.MapPath("~/uSync/Packages");

            if ( File.Exists( Path.Combine(path, "halt.txt")) ) {
                LogHelper.Info(typeof(PackageSync), "Halt stopping") ; 
                return ; 
            }

            if (Directory.Exists(path))
            {
                LogHelper.Info(typeof(PackageSync), "Reading packages from disk"); 

                List<uSyncPackageInfo> uSyncPackages = new List<uSyncPackageInfo>();
                Installer _installer = new Installer();  

                foreach (string packageFile in Directory.GetFiles(path, "*.config"))
                {
                    uSyncPackageInfo package = uSyncPackageInfo.Import(packageFile);

                    if ((package.Status == "new") && (InstalledPackage.isPackageInstalled(package.PackageGuid))) 
                    {
                        // this isn't new. we should mark it as installed. 
                        LogHelper.Info(typeof(PackageSync), String.Format("{0} is already installed setting complete", package.Name));
                        package.Status = "complete" ;
                        package.Save(); 
                    }

                    if (package.Status != "complete")
                    {
                        uSyncPackages.Add(package);
                        LogHelper.Info(typeof(PackageSync), String.Format("Added {0} to list", package.Name));
                    }

                }
 
                //
                // 2: process the packages.
                //

                // logic, we do the stages backwards.. (it does mean multiple loops.. but they should always be small)
                LogHelper.Info(typeof(PackageSync), "Installing packages"); 

                foreach (uSyncPackageInfo pack in uSyncPackages)
                {



                    LogHelper.Info(typeof(PackageSync), String.Format("Installion steps for {0}", pack.Name));
                    if (pack.Status == "business")
                    {
                        PackageBusinessActions(pack, _installer); 
                    }
                }

                bool reboot = false;

                foreach (uSyncPackageInfo pack in uSyncPackages)
                {
                    LogHelper.Info(typeof(PackageSync), String.Format("Package actions for {0}", pack.Name)); 
                    if (reboot == false)
                    {
                        if (pack.Status == "new")
                        {
                            reboot = PackageFileActions(pack, _installer); 
                        }
                    }
                }
            }
        }

        public bool PackageFileActions(uSyncPackageInfo pack, Installer _installer)
        {
            LogHelper.Info(typeof(PackageSync), String.Format("Performing File Actions for {0}", pack.Name));

            bool reboot = false; 
            Repository _repo = Repository.getByGuid(pack.RepoGuid);

            if (_repo.HasConnection())
            {

                LogHelper.Info(typeof(PackageSync), "Repo Has Connection");

                umbraco.cms.businesslogic.packager.repositories.Package p = _repo.Webservice.PackageByGuid(pack.PackageGuid);

                string file = _repo.fetch(pack.PackageGuid);
                LogHelper.Info(typeof(PackageSync), String.Format("Fetched {0}", file));

                if (!String.IsNullOrEmpty(file))
                {
                    string import = _installer.Import(file);
                    LogHelper.Info(typeof(PackageSync), String.Format("Install {0}", import));

                    _installer.LoadConfig(import);

                    int pId = _installer.CreateManifest(import, pack.PackageGuid, pack.RepoGuid);

                    LogHelper.Info(typeof(PackageSync), String.Format("Installing Files for {0}", pack.Name));
                    _installer.InstallFiles(pId, import);

                    pack.PackageId = pId;
                    pack.TempPackageFolder = import;

                    pack.Status = "business";
                    pack.Save();

                    if (_installer.ContainsUnsecureFiles)
                    {
                        LogHelper.Info(typeof(PackageSync), String.Format("Package {0} has dll's will reboot...", pack.Name));
                        // we are going to reboot, because things have changed. 
                        // best thing to do is wait
                        System.Threading.Thread.Sleep(2000);
                        reboot = true; // we'll set this true, we don't want to do any more packages this pass.
                    }
                    else
                    {
                        // do the buisness stuff...
                        PackageBusinessActions(pack, _installer); 
                    }
                }
            }
            return reboot; 
        }

        public bool PackageBusinessActions(uSyncPackageInfo pack, Installer _installer)
        {

            LogHelper.Info(typeof(PackageSync), String.Format("performing business logic for {0}", pack.Name));
            _installer.LoadConfig(pack.TempPackageFolder);
            _installer.InstallBusinessLogic(pack.PackageId, pack.TempPackageFolder);

            pack.Status = "custom";
            pack.Save();

            if (!String.IsNullOrEmpty(_installer.Control))
            {
                // custom bit - not sure how this is going to work ? 
                // because you can't really do it silently
            }

            pack.Status = "complete";
            pack.Save();

            // clean up...
            _installer.InstallCleanUp(pack.PackageId, pack.TempPackageFolder);
            BizLogicAction.ReRegisterActionsAndHandlers();

            return true; 
        }
    }

    public class uSyncPackageInfo
    {
        public string Name { get; set; }
        public string PackageGuid { get; set; }
        public string RepoGuid { get; set; }
        public string Status { get; set; }

        /// <summary>
        ///  things that appear once the package has been done once...
        /// </summary>
        public int PackageId { get; set; }
        public string TempPackageFolder { get; set; }

        public uSyncPackageInfo()
        {
            PackageId = -1;
            Status = "new";
            TempPackageFolder = ""; 
        }

        public uSyncPackageInfo(string n, string pGuid, string rGuid)
        {
            RepoGuid = rGuid;
            PackageGuid = pGuid;
            Name = n;
            PackageId = -1;
            Status = "new";
            TempPackageFolder = ""; 
        }


        public void Save()
        {
            XElement xml = Export();

            if ( !System.IO.Directory.Exists( IOHelper.MapPath("~/uSync/Packages") ) )
                System.IO.Directory.CreateDirectory(IOHelper.MapPath("~/uSync/Packages")) ; 
            
            xml.Save( IOHelper.MapPath(String.Format("~/uSync/Packages/{0}.config", this.Name)));
            LogHelper.Info(typeof(uSyncPackageInfo), String.Format("Saved {0} {1} {2}", this.Name, this.PackageId, this.TempPackageFolder)); 
        }

        private XElement Export()
        {
            // saves this package to disk...
            XElement xml = new XElement("uSyncPackage");

            xml.Add(new XAttribute("name", this.Name)); 
            xml.Add(new XAttribute("packageGuid", this.PackageGuid)) ; 
            xml.Add(new XAttribute("repoGuid", this.RepoGuid)) ; 
            xml.Add(new XAttribute("status", this.Status)) ; 
            xml.Add(new XAttribute("packageId", this.PackageId)) ; 
            xml.Add(new XAttribute("tempFolder", String.IsNullOrEmpty(this.TempPackageFolder) ? "" : this.TempPackageFolder )) ;

            return xml; 
        }

        public static uSyncPackageInfo Import(string file)
        {
              XElement element = XElement.Load(file);

              if (element != null)
              {

                  uSyncPackageInfo newPackage = new uSyncPackageInfo();

                  newPackage.Name = element.Attribute("name").Value; 
                  newPackage.PackageGuid = element.Attribute("packageGuid").Value;
                  newPackage.RepoGuid = element.Attribute("repoGuid").Value;
                  newPackage.Status = element.Attribute("status").Value;
                  newPackage.PackageId = int.Parse(element.Attribute("packageId").Value);
                  newPackage.TempPackageFolder = element.Attribute("tempFolder").Value;

                  return (newPackage);
              }
              return null; 
        }
    }
}
