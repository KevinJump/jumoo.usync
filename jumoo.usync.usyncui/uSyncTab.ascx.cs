using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using jumoo.usync.content;
// using jumoo.usync.packages;

using System.Diagnostics;  

namespace jumoo.usync.usyncui
{
    public partial class uSyncTab : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }


        protected void fullImport_Click(object sender, EventArgs e)
        {
            Stopwatch sw = Stopwatch.StartNew();

            ContentImporter ci = new ContentImporter(); 
            int contentCount = ci.ImportDiskContent();

            MediaImporter mi = new MediaImporter();
            int mediaCount = mi.ImportMedia();

            // we map ids at the end..
            ci.MapContentIds(); 

            sw.Stop(); 

            lblStatus.Text = String.Format("Imported {0} content items and {1} media files [{2} seconds]", 
                contentCount, mediaCount, sw.Elapsed.TotalSeconds); 
        }

        protected void fullExport_Click(object sender, EventArgs e)
        {

            Stopwatch sw = Stopwatch.StartNew();

            ContentExporter cw = new ContentExporter(); 
            int contentCount = cw.ExportSite(false);

            MediaExporter me = new MediaExporter();
            int mediaCount = me.Export(); 

            sw.Stop(); 

            lblStatus.Text = String.Format("Exported {0} content nodes and {1} media items [{2} seconds]", 
                contentCount, mediaCount, sw.Elapsed.TotalSeconds); 
        }
    }
}