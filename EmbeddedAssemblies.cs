using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DeepTools
{
    // Managed-зависимости (LibreHardwareMonitorLib и её спутники) встроены в exe
    // как ресурсы через build.ps1 (/resource:...). Когда CLR не находит сборку рядом
    // с exe, этот обработчик достаёт её из ресурсов и грузит прямо из памяти -
    // так рядом с exe не нужно раскладывать десяток .dll и релиз - один файл.
    public static class EmbeddedAssemblies
    {
        private static readonly object sync = new object();
        private static readonly Dictionary<string, Assembly> cache =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private static bool installed = false;

        public static void Install()
        {
            if (installed) return;
            installed = true;
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            // Имя вида "LibreHardwareMonitorLib, Version=..., Culture=..." - берём простое имя
            string shortName = new AssemblyName(args.Name).Name;
            string resourceName = "DeepTools.Embedded." + shortName + ".dll";

            lock (sync)
            {
                Assembly cached;
                if (cache.TryGetValue(resourceName, out cached)) return cached;

                Assembly self = Assembly.GetExecutingAssembly();
                using (Stream stream = self.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // Сборки с таким именем среди ресурсов нет - пусть CLR ищет дальше сам
                        cache[resourceName] = null;
                        return null;
                    }

                    byte[] data = new byte[stream.Length];
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        int read = stream.Read(data, offset, data.Length - offset);
                        if (read <= 0) break;
                        offset += read;
                    }

                    Assembly loaded = Assembly.Load(data);
                    cache[resourceName] = loaded;
                    return loaded;
                }
            }
        }
    }
}
