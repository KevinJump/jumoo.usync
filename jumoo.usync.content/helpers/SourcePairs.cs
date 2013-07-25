using System;
using System.Collections.Generic;

using System.IO ; 
using System.Xml;
using System.Xml.Linq ;

namespace jumoo.usync.content.helpers
{
    public class SourcePairs
    {
        public static Dictionary<int, Guid> pairs = new Dictionary<int, Guid>();
        public static string pairFile;

        static SourcePairs()
        {
            pairFile = Path.Combine(FileHelper.uSyncRoot, "_source.xml");
        }

        public static void SavePair(int id, Guid key)
        {
            if (!pairs.ContainsKey(id))
            {
                pairs.Add(id, key);
            }
        }

        public static void LoadFromDisk()
        {
            if (File.Exists(pairFile))
            {
                XElement source = XElement.Load(pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach (var pair in sourcePairs)
                {
                    pairs.Add(
                        int.Parse(pair.Attribute("id").Value),
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

            foreach (KeyValuePair<int, Guid> pair in pairs)
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
    }
}
