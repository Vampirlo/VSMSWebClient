using System.IO;
using System.Text;

namespace VSMSWebClient.Services
{
    public class IniFileService
    {
        private readonly string _filePath;

        public IniFileService(string filePath = "VSMSWebClientSettings.ini")
        {
            _filePath = filePath;
            EnsureIniFileExists();
        }

        private void EnsureIniFileExists()
        {
            if (!File.Exists(_filePath))
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var writer = new StreamWriter(_filePath, false, Encoding.UTF8);
                writer.WriteLine("[VSMSWebServer]");
                writer.WriteLine("ip=");
                writer.WriteLine("port=");

                writer.WriteLine("[VSMSWebClient]");
                writer.WriteLine("port=");
                writer.WriteLine("localhost=");
                writer.WriteLine("backgroundInterval=1");
            }
        }

        public string ReadValue(string section, string key)
        {
            try
            {
                var lines = File.ReadAllLines(_filePath);
                bool inSection = false;

                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith($"[{section}]"))
                    {
                        inSection = true;
                        continue;
                    }

                    if (inSection && line.Trim().StartsWith("["))
                    {
                        break; // Another section has started
                    }

                    if (inSection && line.Trim().StartsWith($"{key}="))
                    {
                        return line.Split('=')[1].Trim();
                    }
                }
            }
            catch
            {
                // In case of an error, we will return an empty string
            }

            return string.Empty;
        }

        public void WriteValue(string section, string key, string value)
        {
            EnsureIniFileExists();

            var lines = new List<string>();
            if (File.Exists(_filePath))
            {
                lines = File.ReadAllLines(_filePath).ToList();
            }

            bool sectionFound = false;
            bool keyFound = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Equals($"[{section}]"))
                {
                    sectionFound = true;
                    continue;
                }

                if (sectionFound && lines[i].Trim().StartsWith($"{key}="))
                {
                    lines[i] = $"{key}={value}";
                    keyFound = true;
                    break;
                }

                if (sectionFound && lines[i].Trim().StartsWith("["))
                {
                    break; // Another section has started
                }
            }

            if (!sectionFound)
            {
                lines.Add($"[{section}]");
                lines.Add($"{key}={value}");
            }
            else if (!keyFound)
            {
                int sectionIndex = lines.FindIndex(l => l.Trim().Equals($"[{section}]"));
                if (sectionIndex >= 0 && sectionIndex + 1 < lines.Count)
                {
                    lines.Insert(sectionIndex + 1, $"{key}={value}");
                }
            }

            File.WriteAllLines(_filePath, lines, Encoding.UTF8);
        }

        public int ReadIntValue(string section, string key, int defaultValue = 0)
        {
            var value = ReadValue(section, key);
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}