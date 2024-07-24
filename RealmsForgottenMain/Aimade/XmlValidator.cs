using System.Collections.Generic;
using System.Xml;

namespace RealmsForgotten.AiMade;

public class XmlValidator
{
    public List<string> ErrorMessages { get; } = new List<string>();

    public bool ValidateXml(string filePath)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(filePath);
        XmlNodeList items = xmlDoc.SelectNodes("//Item");

        bool isValid = true;
        foreach (XmlNode item in items)
        {
            if (!ValidateItem(item))
            {
                ErrorMessages.Add($"Invalid item detected: {item.OuterXml}");
                isValid = false;
            }
        }
        return isValid;
    }

    private bool ValidateItem(XmlNode item)
    {
        if (string.IsNullOrEmpty(item.Attributes?["id"]?.InnerText) ||
            string.IsNullOrEmpty(item.Attributes?["name"]?.InnerText))
        {
            return false;
        }
        return true;
    }
}