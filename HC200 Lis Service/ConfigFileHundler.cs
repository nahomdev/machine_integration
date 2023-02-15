using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace HC200_Lis_Service
{
    class ConfigFileHundler
    {
        private static object GetAppSetting(string PropertyName, object DefaultValue, Dictionary<string, object> SettingsDictionary)
        {
            if (!SettingsDictionary.ContainsKey(PropertyName))
                return DefaultValue;

            object temp = SettingsDictionary[PropertyName];
            if (temp != null && temp is System.Text.Json.JsonElement)
            {
                System.Text.Json.JsonElement e = (System.Text.Json.JsonElement)temp;
                switch (e.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.String:
                        return e.ToString();
                    case System.Text.Json.JsonValueKind.Number:
                        return e.GetDouble();
                    case System.Text.Json.JsonValueKind.Null:
                        return null;
                    case System.Text.Json.JsonValueKind.False:
                        return false;
                    case System.Text.Json.JsonValueKind.True:
                        return true;
                        
                }
            }

            return SettingsDictionary[PropertyName];
        }

        public String GetFolderPathToSendOrders()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);


            // actual setting configs here:
            string folderPathToSendOrders = (string)GetAppSetting("folderPathToSendOrders", null, AppSettings);
            
            return folderPathToSendOrders;

            }

        public String GetFolderPathToReadResult()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);


            
            string folderPathToReadResult = (string)GetAppSetting("folderPathToReadResult", null, AppSettings);

            return folderPathToReadResult;
            }

        public String getPathToLogs()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);


         
            string pathToLogFiles = (string)GetAppSetting("pathToLogFiles", null, AppSettings);





            return pathToLogFiles;
           }
        public string GetCmsApi()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);


            string ip_address = (string)GetAppSetting("ip_address", null, AppSettings);
            string port = (string)GetAppSetting("port", null, AppSettings);
            string orbit_cms_api = (string)GetAppSetting("orbit_cms_api", null, AppSettings);

            return orbit_cms_api;
            }
        public string GetIpAddress()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);

 
            string ip_address = (string)GetAppSetting("ip_address", null, AppSettings);

            return ip_address;
        }

        public string GetLocalFolder()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);


            string local_folder = (string)GetAppSetting("localFolderForOrders", null, AppSettings);

            return local_folder;
        }
        public string GetPort()
        {
            string AppSettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string AppSettingsText = File.ReadAllText(AppSettingsFile);
            Dictionary<string, object> AppSettings = null;
            AppSettings = new Dictionary<string, object>(
               System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
               AppSettingsText), StringComparer.CurrentCultureIgnoreCase);

            string port = (string)GetAppSetting("port", null, AppSettings);

            return port;
        }
    }
}
