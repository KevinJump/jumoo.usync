using System;
using System.Collections.Generic;
using System.Linq; 

using System.IO; 
using System.Xml;
using System.Xml.Linq;

namespace jumoo.usync.content.helpers
{
    /// <summary>
    ///  Import pairs, takes the GUID that a peice of content arrives at our install with
    ///  and maps it to the GUID on our system.
    ///  
    ///  when ever a guid is then written out by uSync, it is always set back to the master
    ///  GUID (that is the GUID of the umbraco install that created the content first) that
    ///  way mapping will work across all umbraco installs, as long as they all agree on 
    ///  which install created a peice of content. 
    /// </summary>
    public class ImportPairs
    {
        public static Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
        public static string pairFile;

        static ImportPairs()
        {
            pairFile = Path.Combine(FileHelper.uSyncTemp, "_uSyncImport.xml");
        }


        public static void SavePair(Guid id, Guid key)
        {
            if (pairs.ContainsKey(id))
            {
                pairs.Remove(id); 
            }
            pairs.Add(id, key);
        }

        public static void LoadFromDisk()
        {
            // blank it. 
            pairs = new Dictionary<Guid, Guid>(); 


            if (File.Exists(pairFile))
            {
                XElement source = XElement.Load(pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach (var pair in sourcePairs)
                {
                    pairs.Add(
                        Guid.Parse(pair.Attribute("id").Value),
                        Guid.Parse(pair.Attribute("guid").Value));
                }
            }
        }

        public static void SaveToDisk()
        {
            if (File.Exists(pairFile))
                File.Delete(pairFile);

            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            doc.AppendChild(dec);

            XmlElement data = doc.CreateElement("usync.data");
            XmlElement content = doc.CreateElement("content");
            
            foreach (KeyValuePair<Guid, Guid> pair in pairs)
            {
                XmlElement p = doc.CreateElement("pair");
                p.SetAttribute("id", pair.Key.ToString());
                p.SetAttribute("guid", pair.Value.ToString());

                content.AppendChild(p);
            }

            data.AppendChild(content);
            doc.AppendChild(data);
            doc.Save(pairFile);
        }


        /// <summary>
        ///  maps a guid to the orginal source (master) guid from the system
        ///  that created this content
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static Guid GetSourceGuid(Guid guid)
        {
            if ( pairs.ContainsValue(guid) )
                return pairs.FirstOrDefault(x => x.Value == guid).Key;

            return guid ; 
        }

        /// <summary>
        ///  looks for a match in our import table for the guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static Guid GetTargetGuid(Guid guid)
        {
            if ( pairs.ContainsKey(guid) )
                return pairs[guid] ; 

            return guid ; 
        }

        private static string GetInnerXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            string xml = reader.ReadInnerXml();

            // except umbraco then doesn't like content
            // starting cdata
            if (xml.StartsWith("<![CDATA["))
            {
                return parent.Value;
            }
            return xml;
        }
    }
}

