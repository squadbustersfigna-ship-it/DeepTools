using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeepTools
{
    // Загрузка GPU через счётчики производительности Windows.
    // У категории "GPU Engine" нет инстанса "_Total" - есть отдельные инстансы на
    // каждый процесс/движок. Реальную загрузку даёт сумма 3D-движков (engtype_3D).
    // Счётчики держим постоянно и обновляем набор инстансов раз в несколько секунд,
    // т.к. процессы приходят и уходят
    public static class GpuLoad
    {
        private static readonly object sync = new object();
        private static List<PerformanceCounter> counters = new List<PerformanceCounter>();
        private static DateTime lastRefresh = DateTime.MinValue;
        private static bool categoryOk = true;

        // Загрузка GPU в %, или -1 если счётчики недоступны
        public static float Get()
        {
            lock (sync)
            {
                try
                {
                    if ((DateTime.Now - lastRefresh).TotalSeconds > 5) Refresh();
                    if (!categoryOk) return -1;

                    float sum = 0;
                    for (int i = 0; i < counters.Count; i++)
                    {
                        try { sum += counters[i].NextValue(); } catch { }
                    }
                    if (sum < 0) sum = 0;
                    if (sum > 100) sum = 100;
                    return sum;
                }
                catch { return -1; }
            }
        }

        private static void Refresh()
        {
            lastRefresh = DateTime.Now;
            for (int i = 0; i < counters.Count; i++) { try { counters[i].Dispose(); } catch { } }
            counters.Clear();

            try
            {
                var cat = new PerformanceCounterCategory("GPU Engine");
                string[] names = cat.GetInstanceNames();
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    try { counters.Add(new PerformanceCounter("GPU Engine", "Utilization Percentage", names[i], true)); }
                    catch { }
                }
                categoryOk = true;
            }
            catch
            {
                categoryOk = false;
            }
        }
    }
}
