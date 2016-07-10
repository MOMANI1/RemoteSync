using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;

namespace Common
{
    public static class AppSettings
    {
        public static T Get<T>(string key)
        {
            var appSetting = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(appSetting)) throw new AppSettingNotFoundException(key);

            if (key == "TempDirectoryPath" && appSetting== "default")
            {
                appSetting = Path.GetTempPath();
            }
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)(converter.ConvertFromInvariantString(appSetting));
        }
    }

    public class AppSettingNotFoundException : Exception
    {
        public AppSettingNotFoundException()
        {
        }
        public AppSettingNotFoundException(string key)
            : base("AppSettingNotFound for " + key)
        {
        }
        public AppSettingNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}