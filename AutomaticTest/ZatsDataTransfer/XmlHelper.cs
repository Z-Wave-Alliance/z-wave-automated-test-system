// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Zats.Services
{
    public static class XmlHelper
    {
        public static bool ObjectToXmlFile<T>(T dataList, string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folderPath) && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            bool ret = false;
            XmlSerializer zwClassXmlSerializer = new XmlSerializer(typeof(T));

            XmlWriterSettings sett = new XmlWriterSettings();
            sett.Indent = true;
            sett.NewLineChars = Environment.NewLine;
            XmlSerializerNamespaces names = new XmlSerializerNamespaces();
            names.Add(String.Empty, string.Empty);
            XmlWriter writer = XmlWriter.Create(filePath, sett);
            try
            {
                zwClassXmlSerializer.Serialize(writer, dataList, names);
                ret = true;
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Exception catched in ZatsDataToXmlFile:");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                writer.Close();
            }
            return ret;
        }

        public static T XmlFileToObject<T>(string xmlPath)
        {
            T xmlObject = default(T);
            // check that XML file exists
            if (File.Exists(xmlPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                try
                {
                    // deserialize XML file
                    using (FileStream sReader = new FileStream(xmlPath, FileMode.Open))
                    {
                        xmlObject = (T)serializer.Deserialize(sReader);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Exception catched in XmlFileToObject<T>:");
                    Console.WriteLine(e.ToString());
                    return default(T);
                }
            }
            return xmlObject;
        }
    }
}
