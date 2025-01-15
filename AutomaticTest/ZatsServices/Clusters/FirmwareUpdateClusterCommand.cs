// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace UicApplication.Clusters
{
    public class FirmwareUpdateClusterCommand
    {
        Root JsonData = new Root();
        List<Image> jImage = new List<Image>();

        public List<Image> SetImagesData(string jsonUnid, string FileName, string FwMd5)
        {            
            Image myImageData = new Image();
            myImageData.FileName = FileName;
            myImageData.Uiid = @"ZWave-0000-0002-0004-00-01"; //Switch OnOff
            myImageData.Unid = new List<string> { jsonUnid };
            myImageData.Version = @"255.0.0";
            myImageData.ApplyAfter = System.DateTime.Today;
            myImageData.Md5 = FwMd5;
            jImage.Add(myImageData);            
            return jImage;
        }
        public void CreateLocalJson()
        {     
            JsonData.Version = "1";
            JsonData.Images = jImage;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(JsonData, Newtonsoft.Json.Formatting.Indented);
            var res = AppDomain.CurrentDomain.BaseDirectory;
            File.WriteAllText(res + @"images.json", output);
            
        }
        public void UpdateHomeIdToJson(string NewHomeId)
        {
            Console.WriteLine("End device Home ID: " + NewHomeId);

            //Read file to string
            string jsonfile = File.ReadAllText("images.json");

            //Deserialize from file to object:
            var root = new Root();
            JsonConvert.PopulateObject(jsonfile, root);

            //Change Value
            root.Images[0].Unid = new List<string> { NewHomeId };
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(root, Newtonsoft.Json.Formatting.Indented);

            //Serialize JSON directly to a file again
            File.WriteAllText("images.json", output);

            ////Serialize JSON directly to a file again
            //using (StreamWriter file = File.CreateText(@"images.json"))
            //{
            //    JsonSerializer serializer = new JsonSerializer();
            //    serializer.Serialize(file, root);
            //}
        }     

    }

    #region image Json class
    public class Image
    {
        public string FileName { get; set; }
        public string Uiid { get; set; }
        public List<string> Unid { get; set; }
        public string Version { get; set; }
        public DateTime ApplyAfter { get; set; }
        public string Md5 { get; set; }
    }

    public class Root
    {
        public string Version { get; set; }
        public List<Image> Images { get; set; }
    }
    #endregion
}
