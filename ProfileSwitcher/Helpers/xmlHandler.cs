using System;
using System.Text;
using System.Xml;
using System.IO;
using Zeta.Common;
using System.Globalization;

namespace ProfileSwitcher.Helpers
{
    public static class xmlHandler
    {
        public static void readXml(XmlTextReader xml)
        {
            bool error = false;
            profileHandler.profileWaypoints.Clear();
            while (xml.Read())
            {
                switch (xml.NodeType)
                {
                    case XmlNodeType.Element:
                        Vector3 waypoint = new Vector3();
                        if (xml.Name == "MoveTo" || xml.Name == "UseWaypoint" || xml.Name == "UseObject")
                        {
                            try
                            {
                                xml.MoveToAttribute("x");
                                waypoint.X = float.Parse(xml.Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                            }
                            catch (Exception e)
                            {
                                ProfileSwitcher.Log(String.Format("x coordinate can't be parsed from xml; Element: {0} Line: {1} Position: {2}", xml.Name, xml.LineNumber, xml.LinePosition));
                                error = true;
                            }
                            try
                            {
                                xml.MoveToAttribute("y");
                                waypoint.Y = float.Parse(xml.Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                            }
                            catch (Exception e)
                            {
                                ProfileSwitcher.Log(String.Format("y coordinate can't be parsed from xml; Element: {0} Line: {1} Position: {2}", xml.Name, xml.LineNumber, xml.LinePosition));
                                error = true;
                            }
                            try
                            {
                                xml.MoveToAttribute("z");
                                waypoint.Z = float.Parse(xml.Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                            }
                            catch (Exception e)
                            {
                                ProfileSwitcher.Log(String.Format("z coordinate can't be parsed from xml; Element: {0} Line: {1} Position: {2}", xml.Name, xml.LineNumber, xml.LinePosition));
                                error = true;
                            }
                        }
                        if (!error)
                        {
                            profileHandler.profileWaypoints.Add(waypoint);
                        }
                        else
                        {
                            error = false;
                            ProfileSwitcher.Log("Ignoring Waypoint");
                        }
                        break;

                }

            }

        }

        public static string generateTempProfile(Vector3 nearestWaypoint, string currentProfile, string nextProfile)
        {
            bool indexWritten = false;
            bool foundWP = false;
            bool error = false;
            string[] whileAttrNames = new string[5];
            string[] whileAttrValues = new string[5];
            int j = 0;
            int ifindex = 0;
            int whileIndex = 0;
            DateTime currentDateTime = DateTime.Now;
            String dateStr = currentDateTime.ToString("yyyy-MM-dd HH_mm_ss");
            string tmpProfilePath = Path.GetTempPath() + dateStr + ".xml";
            XmlTextReader origProfile = new XmlTextReader(currentProfile);
            XmlTextWriter tempProfile = new XmlTextWriter(tmpProfilePath, null);
            tempProfile.Formatting = Formatting.Indented;
            tempProfile.WriteStartDocument();
            while (origProfile.Read())
            {
                switch (origProfile.NodeType)
                {
                    case XmlNodeType.Element:
                        tempProfile.Flush();
                        if (origProfile.Name == "Order")
                        {
                            tempProfile.WriteStartElement(origProfile.Name);
                            indexWritten = true;
                            break;
                        }
                        if (foundWP)
                        {
                            if (origProfile.Name == "nextProfile" || origProfile.Name == "Continue")
                            {
                                tempProfile.WriteStartElement("nextProfile");
                                tempProfile.WriteAttributeString("path", nextProfile);
                                tempProfile.WriteEndElement();
                                break;
                            }
                            tempProfile.WriteStartElement(origProfile.Name);
                            while (origProfile.MoveToNextAttribute())
                            {
                                tempProfile.WriteAttributeString(origProfile.Name, origProfile.Value);
                            }
                            origProfile.MoveToElement();
                            if (origProfile.Name != "If" && origProfile.Name != "While" && origProfile.Name != "KillMonsters" && origProfile.Name != "PickupLoot" && origProfile.Name != "TargetBlacklists")
                            {
                                tempProfile.WriteEndElement();
                            }
                            break;
                        }
                        if (!indexWritten)
                        {
                            tempProfile.WriteStartElement(origProfile.Name);
                            if (origProfile.HasAttributes)
                            {
                                while (origProfile.MoveToNextAttribute())
                                {
                                    tempProfile.WriteAttributeString(origProfile.Name, origProfile.Value);
                                }
                            }
                            origProfile.MoveToElement();
                            if (origProfile.Name == "GameParams" || origProfile.Name == "TargetBlacklist")
                            {
                                tempProfile.WriteEndElement();
                            }
                            break;
                        }
                        if (indexWritten && origProfile.Name == "If" && !foundWP)
                        {
                            ifindex++;
                            break;
                        }
                        if (indexWritten && origProfile.Name == "While" && !foundWP)
                        {
                            Array.Clear(whileAttrNames, 0, whileAttrNames.Length);
                            Array.Clear(whileAttrValues, 0, whileAttrValues.Length);
                            if (origProfile.HasAttributes)
                            {
                                for (int i = 0; i < origProfile.AttributeCount; i++)
                                {
                                    origProfile.MoveToNextAttribute();
                                    whileAttrNames[i] = origProfile.Name;
                                    whileAttrValues[i] = origProfile.Value;
                                }
                            }
                            whileIndex = 1;
                            break;
                        }
                        if (indexWritten && (origProfile.Name == "MoveTo" || origProfile.Name == "UseWaypoint" || origProfile.Name == "UseObject") && !foundWP)
                        {
                            ProfileSwitcher.Log("Searching WP..." + ++j);
                            Vector3 waypoint = new Vector3();
                            error = false;
                            try
                            {
                                origProfile.MoveToElement();
                                origProfile.MoveToAttribute("x");
                                waypoint.X = float.Parse(origProfile.Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                                origProfile.MoveToElement();
                                origProfile.MoveToAttribute("y");
                                waypoint.Y = float.Parse(origProfile.Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                                origProfile.MoveToElement();
                                origProfile.MoveToAttribute("z");
                                waypoint.Z = float.Parse(origProfile.Value, System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                            }
                            catch(Exception e)
                            {
                                error = true;
                            }
                            origProfile.MoveToElement();
                            if (!error && (waypoint == nearestWaypoint))
                            {
                                ProfileSwitcher.Log("Found WP:" + waypoint.ToString());
                                if (whileIndex == 1)
                                {
                                    tempProfile.WriteStartElement("While");
                                    for (int i = 0; i < whileAttrNames.Length; i++)
                                    {
                                        if (whileAttrNames[i] != null)
                                        {
                                            tempProfile.WriteAttributeString(whileAttrNames[i], whileAttrValues[i]);
                                        }
                                    }
                                    whileIndex = 0;
                                }
                                tempProfile.WriteStartElement(origProfile.Name);
                                while (origProfile.MoveToNextAttribute())
                                {
                                    tempProfile.WriteAttributeString(origProfile.Name, origProfile.Value);
                                }
                                foundWP = true;
                                tempProfile.WriteEndElement();
                            }
                            else
                            {
                                //ProfileSwitcher.Log("Wrong waypoint: " + waypoint.ToString());
                            }
                            break;
                        }
                        break;
                    case XmlNodeType.Text:
                        if (!indexWritten || foundWP)
                        {
                            tempProfile.WriteString(origProfile.Value);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (indexWritten && !foundWP && (ifindex > 0))
                        {
                            ifindex--;
                            break;
                        }
                        if (indexWritten && !foundWP && (whileIndex == 1))
                        {
                            whileIndex = 0;
                            Array.Clear(whileAttrNames, 0, whileAttrNames.Length);
                            Array.Clear(whileAttrValues, 0, whileAttrValues.Length);
                            break;
                        }
                        if (!indexWritten || foundWP)
                        {
                            if (ifindex > 0)
                            {
                                ifindex--;
                                break;
                            }
                            tempProfile.WriteFullEndElement();
                        }
                        break;
                }
            }
            tempProfile.WriteEndDocument();
            tempProfile.Flush();
            tempProfile.Close();
            if (foundWP)
            {
                return tmpProfilePath;
            }
            else
            {
                return null;
            }
        }
    }
}