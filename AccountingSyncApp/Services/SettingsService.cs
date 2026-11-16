using System;
using System.IO;
using System.Text.Json; // استخدام المكتبة المدمجة بدلاً من Newtonsoft
using AccountingSyncAPP.Configuration;

namespace AccountingSyncAPP.Services
{
    public class SettingsService
    {
        private readonly string _filePath;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "AccountingSyncAPP");
            Directory.CreateDirectory(appFolder);
            _filePath = Path.Combine(appFolder, "settings.json");
        }

        public ConnectionSettings? LoadSettings()
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                // استخدام JsonSerializer المدمج
                return JsonSerializer.Deserialize<ConnectionSettings>(json);
            }
            catch
            {
                return null;
            }
        }

        public void SaveSettings(ConnectionSettings settings)
        {
            // إعدادات لجعل ملف الـ Json سهل القراءة (Indented)
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_filePath, json);
        }
    }
}