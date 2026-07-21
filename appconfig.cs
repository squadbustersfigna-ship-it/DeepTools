using System;
using System.Collections.Generic;
using System.IO;

namespace DeepTools
{
    // Простой конфиг вида ключ=значение в файле AppData
    // Не используем XML/JSON сериализацию, чтобы обойтись без внешних библиотек
    public static class AppConfig
    {
        private static string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeepTools");
        private static string FilePath = Path.Combine(FolderPath, "config.txt");
        private static Dictionary<string, string> values = new Dictionary<string, string>();
        private static bool loaded = false;

        public static void Load()
        {
            values.Clear();
            if (File.Exists(FilePath))
            {
                string[] lines = File.ReadAllLines(FilePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int idx = line.IndexOf('=');
                    if (idx > 0)
                    {
                        string key = line.Substring(0, idx);
                        string val = line.Substring(idx + 1);
                        values[key] = val;
                    }
                }
            }
            loaded = true;
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> pair in values)
                {
                    lines.Add(pair.Key + "=" + pair.Value);
                }
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch
            {
                // Если не смогли сохранить настройки - не критично, просто продолжаем без них
            }
        }

        public static string Get(string key, string defaultValue)
        {
            if (!loaded) Load();
            if (values.ContainsKey(key)) return values[key];
            return defaultValue;
        }

        public static void Set(string key, string value)
        {
            if (!loaded) Load();
            values[key] = value;
            Save();
        }

        public static bool GetBool(string key, bool defaultValue)
        {
            string s = Get(key, defaultValue ? "1" : "0");
            return s == "1";
        }

        public static void SetBool(string key, bool value)
        {
            Set(key, value ? "1" : "0");
        }
    }
}