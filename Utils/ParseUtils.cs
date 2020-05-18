using RotMG.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RotMG.Utils
{
    public static class ParseUtils
    {
        public static string ParseString(this XElement element, string name, string undefined = null)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return value;
        }

        public static int ParseInt(this XElement element, string name, int undefined = 0)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return int.Parse(value);
        }

        public static long ParseLong(this XElement element, string name, long undefined = 0)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return long.Parse(value);
        }

        public static uint ParseUInt(this XElement element, string name, uint undefined = 0)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return Convert.ToUInt32(value, 16);
        }

        public static float ParseFloat(this XElement element, string name, float undefined = 0)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public static bool ParseBool(this XElement element, string name, bool undefined = false)
        {
            bool isAttr = name[0].Equals('@');
            string id = name[0].Equals('@') ? name.Remove(0, 1) : name;
            string value = isAttr ? element.Attribute(id)?.Value : element.Element(id)?.Value;
            if (string.IsNullOrWhiteSpace(value)) 
            {
                if ((isAttr && element.Attribute(id) != null) || (!isAttr && element.Element(id) != null))
                    return true;
                return undefined; 
            }
            return bool.Parse(value);
        }

        public static ushort ParseUshort(this XElement element, string name, ushort undefined = 0)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return (ushort)(value.StartsWith("0x") ? Int32.Parse(value.Substring(2), NumberStyles.HexNumber) : Int32.Parse(value));
        }

        public static ConditionEffectIndex ParseConditionEffect(this XElement element, string name, ConditionEffectIndex undefined = ConditionEffectIndex.Nothing)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return undefined;
            return (ConditionEffectIndex)Enum.Parse(typeof(ConditionEffectIndex), value.Replace(" ", ""));
        }

        public static ActivateEffectIndex ParseActivateEffect(this XElement element, string name)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
#if DEBUG
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception("Failed parsing effect.");
#endif
            return (ActivateEffectIndex)Enum.Parse(typeof(ActivateEffectIndex), value.Replace(" ", ""));
        }

        public static string[] ParseStringArray(this XElement element, string name, string seperator, string[] undefined = null)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return value.Split(seperator);
        }

        public static int[] ParseIntArray(this XElement element, string name, string seperator, int[] undefined = null)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator, null).Select(k => int.Parse(k)).ToArray();
        }

        public static ushort[] ParseUshortArray(this XElement element, string name, string seperator, ushort[] undefined = null)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator, null).Select(k => (ushort)(k.StartsWith("0x") ? Int32.Parse(k.Substring(2), NumberStyles.HexNumber) : Int32.Parse(k))).ToArray();
        }

        public static List<int> ParseIntList(this XElement element, string name, string seperator, List<int> undefined = null)
        {
            string value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator, null).Select(k => int.Parse(k)).ToList();
        }
    }
}
