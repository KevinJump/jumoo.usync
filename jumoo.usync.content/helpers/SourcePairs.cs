using System;
using System.Collections.Generic;

using System.IO ; 
using System.Xml;
using System.Xml.Linq ;

namespace jumoo.usync.content.helpers
{
    public class SourcePairs
    {
        public static Dictionary<Guid, int> pairs = new Dictionary<Guid, int>();
        public static string pairFile;

        static SourcePairs()
        {
            pairFile = Path.Combine(FileHelper.uSyncRoot, "_source.xml");
            
        }

        public static void SavePair(Guid key, int id)
        {
            if (!pairs.ContainsKey(key) )
            {
                pairs.Add(key, id);
            }
        }

        public static void LoadFromDisk()
        {
            pairs = new Dictionary<Guid, int>(); 

            if (File.Exists(pairFile))
            {
                XElement source = XElement.Load(pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach (var pair in sourcePairs)
                {
                    pairs.Add(
                        Guid.Parse(pair.Attribute("guid").Value),
                        int.Parse(pair.Attribute("id").Value));
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

            foreach (KeyValuePair<Guid, int> pair in pairs)
            {
                XmlElement p = doc.CreateElement("pair");
                p.SetAttribute("guid", pair.Key.ToString());
                p.SetAttribute("id", pair.Value.ToString());

                content.AppendChild(p); 
            }

            data.AppendChild(content);
            doc.AppendChild(data);
            doc.Save(pairFile); 
        }
    }
}
