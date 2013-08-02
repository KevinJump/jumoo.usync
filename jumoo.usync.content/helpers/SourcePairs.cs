using System;
using System.Collections.Generic;

using System.IO ; 
using System.Xml;
using System.Xml.Linq ;

namespace jumoo.usync.content.helpers
{
    public class SourcePairs
    {
        public static Dictionary<Guid, string> pairs = new Dictionary<Guid, string>();
        public static string pairFile;

        static SourcePairs()
        {
            pairFile = Path.Combine(FileHelper.uSyncRoot, "_sourceNames.xml");
            
        }

        public static void SavePair(Guid key, string name)
        {
            if (pairs.ContainsKey(key) )
                pairs.Remove(key); 

            pairs.Add(key, name);
        }

        public static void LoadFromDisk()
        {
            pairs = new Dictionary<Guid, string>(); 

            if (File.Exists(pairFile))
            {
                XElement source = XElement.Load(pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach (var pair in sourcePairs)
                {
                    pairs.Add(
                        Guid.Parse(pair.Attribute("guid").Value),
                        pair.Attribute("name").Value);
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

            foreach (KeyValuePair<Guid, string> pair in pairs)
            {
                XmlElement p = doc.CreateElement("pair");
                p.SetAttribute("guid", pair.Key.ToString());
                p.SetAttribute("name", pair.Value);

                content.AppendChild(p); 
            }

            data.AppendChild(content);
            doc.AppendChild(data);
            doc.Save(pairFile); 
        }

        public static string GetName(Guid key)
        {
            if (pairs.ContainsKey(key))
                return pairs[key];
            else
                return "";
        }
    }
}
