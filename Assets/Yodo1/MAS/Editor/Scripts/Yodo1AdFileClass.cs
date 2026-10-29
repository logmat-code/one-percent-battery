namespace Yodo1.MAS
{
    using System.IO;
    using System.Text;
    using UnityEngine;

    public class Yodo1AdFileClass
    {
        private readonly string filePath;

        public Yodo1AdFileClass(string fPath)
        {
            filePath = fPath;
            if (!File.Exists(filePath))
            {
                Debug.LogError("The file does not exist in the path: " + filePath);
            }
        }

        public bool ContainsText(string text)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            string content = File.ReadAllText(filePath, Encoding.UTF8);
            return content.Contains(text);
        }

        /// <summary>
        /// Write text below the line containing the specified anchor string.
        /// If the text already exists in the file, it will not be written again.
        /// </summary>
        public void WriteBelow(string below, string text)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            string content = File.ReadAllText(filePath, Encoding.UTF8);

            // Skip if text already exists
            if (content.Contains(text))
            {
                return;
            }

            // Find the anchor string
            int beginIndex = content.IndexOf(below);
            if (beginIndex == -1)
            {
                return;
            }

            // Find the end of the line containing the anchor (search forward for '\n')
            int endIndex = content.IndexOf("\n", beginIndex + below.Length);
            if (endIndex == -1)
            {
                // Anchor is on the last line with no trailing newline
                content += "\n" + text;
            }
            else
            {
                content = content.Substring(0, endIndex) + "\n" + text + content.Substring(endIndex);
            }

            File.WriteAllText(filePath, content, Encoding.UTF8);
        }
    }
}
