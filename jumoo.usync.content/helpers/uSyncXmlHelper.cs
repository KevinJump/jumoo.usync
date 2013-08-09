using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core ;
using Umbraco.Core.Models ;
using Umbraco.Core.Services;
using Umbraco.Core.Logging ; 

using System.Xml;
using System.Xml.Linq;

using System.Text.RegularExpressions;

using jumoo.usync.content.helpers;

namespace jumoo.usync.content.helpers
{
    /// <summary>
    ///  some of the genric stuff. we can do for both media and content
    /// </summary>
    public class uSyncXmlHelper
    {
        public static XElement ExportContentBase(string type, IContentBase item, bool props = true)
        {
            XElement xml = new XElement(type);

            Guid _guid = ImportPairs.GetSourceGuid(item.Key);
            xml.Add(new XAttribute("guid", _guid));

            xml.Add(new XAttribute("id", item.Id));
            xml.Add(new XAttribute("nodeName", item.Name));
            xml.Add(new XAttribute("isDoc", ""));
            xml.Add(new XAttribute("update", item.UpdateDate));

            foreach (var property in item.Properties.Where(p => p != null))
            {
                XElement propXml = property.ToXml();

                string xmlVal = "";
                if (props)
                {
                    xmlVal = ReplaceIdsWithGuid(GetInnerXML(propXml));
                }
                else
                {
                    xmlVal = GetInnerXML(propXml);
                }


                XElement p = XElement.Parse(string.Format("<{0}>{1}</{0}>", propXml.Name.ToString(), propXml));

                xml.Add(p);

            }

            return xml;
        }

  

        #region Helpers 

        private static string ReplaceIdsWithGuid(string propValue)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            // look for things that might be Ids
            foreach (Match m in Regex.Matches(propValue, @"\d{1,10}"))
            {
                Guid? localGuid = GetGuidFromId(int.Parse(m.Value));
                if (localGuid != null)
                {
                    if (!replacements.ContainsKey(m.Value))
                    {
                        Guid sourceGuid = helpers.ImportPairs.GetSourceGuid(localGuid.Value);
                        replacements.Add(m.Value, sourceGuid.ToString().ToUpper());
                    }
                }
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                LogHelper.Info(typeof(ContentWalker), String.Format("Updating Id's {0} > {1}", pair.Key, pair.Value));
                propValue = propValue.Replace(pair.Key, pair.Value);
            }

            LogHelper.Debug(typeof(ContentWalker), String.Format("Updated [{0}]", propValue));
            return propValue;
        }

        private static Guid? GetGuidFromId(int id)
        {
            ContentService cs = new ContentService();

            IContent contentItem = cs.GetById(id);
            if (contentItem != null)
                return contentItem.Key;
            else
                return null;

        }

        private static string GetInnerXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }
        #endregion 

    }
}
