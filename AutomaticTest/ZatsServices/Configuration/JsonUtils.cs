using Newtonsoft.Json;
using System.IO;
using Utils;

namespace Zats.Services
{
    public class JsonUtils
    {
        /// <summary>
        /// Convert JSON string to object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T ToValue<T>(string json)  where T : class
        {
            T ret = null;
            try
            {
                ret = JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonReaderException)
            {

            }
            return ret;
        }

        /// <summary>
        /// Convert object to JSON string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FromValue(object value)
        {
            string ret = JsonConvert.SerializeObject(value);
            return ret;
        }

        /// <summary>
        /// Load JSON file at the specified file or directory path.  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T Load<T>(string path) where T : class
        {
            T ret = null;
            var filePath = path;
            if (Directory.Exists(filePath) && !File.Exists(filePath))
            {
                filePath = Path.Combine(filePath, $"{typeof(T).Name}.json");
            }
            FileAccessMonitor.Read(filePath, () =>
            {
                var str = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(str))
                {
                    try
                    {
                        ret = JsonConvert.DeserializeObject<T>(str);
                    }
                    catch (JsonReaderException ex)
                    {
                        $"{ex.Message}"._DLOG();
                    }
                }
            });
            return ret;
        }

        /// <summary>
        /// Load JSON file at the current directory with type name as the file path.  
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Load<T>() where T : class
        {
            var path = $"{typeof(T).Name}.json";
            return Load<T>(path);
        }

        /// <summary>
        /// Save object to JSON file at the specified file or directory path.  
        /// </summary>
        /// <param name="path"></param>
        /// <param name="value"></param>
        public static void Save(string path, object value)
        {
            string json = JsonConvert.SerializeObject(value, Formatting.Indented);
            var filePath = path;
            if (!File.Exists(filePath) && Directory.Exists(filePath))
            {
                filePath = Path.Combine(filePath, $"{value.GetType().Name}.json");
            }
            if (filePath != null)
            {
                File.WriteAllText(filePath, json);
            }
        }

        /// <summary>
        /// Save object to JSON file at the current directory with type name as the file path.  
        /// </summary>
        /// <param name="value"></param>
        public static void Save(object value)
        {
            var path = $"{value.GetType().Name}.json";
            Save(path, value);
        }
    }
}
