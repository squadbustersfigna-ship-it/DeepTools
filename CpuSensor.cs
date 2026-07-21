using System;
using LibreHardwareMonitor.Hardware;

namespace DeepTools
{
    // Температура CPU через LibreHardwareMonitorLib (тот же движок, что у HWiNFO/LibreHardwareMonitor).
    // Требует прав администратора (у нас всегда есть) - библиотека грузит свой драйвер для чтения датчиков.
    // Инициализация занимает ~1-2 секунды, поэтому делается в фоне через InitAsync()
    public static class CpuSensor
    {
        private static Computer computer;
        private static IHardware cpu;
        private static bool initStarted = false;
        private static readonly object sync = new object();

        public static void InitAsync()
        {
            lock (sync)
            {
                if (initStarted) return;
                initStarted = true;
            }
            var t = new System.Threading.Thread(Init);
            t.IsBackground = true;
            t.Start();
        }

        private static void Init()
        {
            try
            {
                var c = new Computer();
                c.IsCpuEnabled = true;
                c.Open();
                foreach (IHardware hw in c.Hardware)
                {
                    if (hw.HardwareType == HardwareType.Cpu)
                    {
                        lock (sync)
                        {
                            computer = c;
                            cpu = hw;
                        }
                        return;
                    }
                }
                c.Close();
            }
            catch
            {
                // датчики недоступны (виртуалка, экзотический CPU) - будет н/д
            }
        }

        // Температура CPU в °C, или -1 если недоступна.
        // Предпочитаем Package/Tctl/Average - это "общая" температура процессора,
        // иначе берём максимум по ядрам
        public static int GetTemperature()
        {
            IHardware hw;
            lock (sync) { hw = cpu; }
            if (hw == null) return -1;

            try
            {
                hw.Update();
                float best = -1;
                float package = -1;
                foreach (ISensor s in hw.Sensors)
                {
                    if (s.SensorType != SensorType.Temperature || !s.Value.HasValue) continue;
                    float v = s.Value.Value;
                    if (v <= 0 || v > 120) continue;

                    string n = s.Name == null ? "" : s.Name;
                    if (n.IndexOf("Package", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Tctl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Average", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (v > package) package = v;
                    }
                    if (v > best) best = v;
                }
                if (package > 0) return (int)Math.Round(package);
                if (best > 0) return (int)Math.Round(best);
            }
            catch { }
            return -1;
        }

        public static void Shutdown()
        {
            lock (sync)
            {
                try { if (computer != null) computer.Close(); } catch { }
                computer = null;
                cpu = null;
            }
        }
    }
}
