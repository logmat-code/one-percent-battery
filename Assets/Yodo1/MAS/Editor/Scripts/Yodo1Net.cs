namespace Yodo1.MAS
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Typed model for the app info returned by the MAS server.
    /// Both /setup/{appKey} and /get-app-info-by-bundle-id share this structure.
    /// </summary>
    public class Yodo1AppInfo
    {
        public string Platform { get; private set; }
        public string AppKey { get; private set; }
        public string AdmobKey { get; private set; }
        public string BundleId { get; private set; }
        public string Name { get; private set; }

        public bool IsIOS => string.Equals(Platform, "ios", StringComparison.OrdinalIgnoreCase);
        public bool IsAndroid => string.Equals(Platform, "android", StringComparison.OrdinalIgnoreCase);

        public bool HasRequiredFields =>
            !string.IsNullOrEmpty(AppKey) &&
            !string.IsNullOrEmpty(AdmobKey) &&
            !string.IsNullOrEmpty(BundleId);

        internal static Yodo1AppInfo Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var dic = Yodo1JSON.Deserialize(json) as Dictionary<string, object>;
            if (dic == null)
            {
                return null;
            }

            return new Yodo1AppInfo
            {
                Platform = GetString(dic, "platform"),
                AppKey = GetString(dic, "app_key"),
                AdmobKey = GetString(dic, "admob_key"),
                BundleId = GetString(dic, "bundle_id"),
                Name = GetString(dic, "name"),
            };
        }

        private static string GetString(Dictionary<string, object> dic, string key)
        {
            if (dic.TryGetValue(key, out object value))
            {
                return value as string ?? string.Empty;
            }
            return string.Empty;
        }
    }

    public class Yodo1Net
    {
        private const string BASE_URL = "https://sdk-mas.yodo1.com/v1/unity";
        private const string SECRET_KEY = "W4OJaaOVAmO2uGaUVCCw24cuHKu4Zc";
        private const int REQUEST_TIMEOUT_MS = 15000;

        private static Yodo1Net instance;

        static Yodo1Net()
        {
            // Unity 2018~2021 with the legacy Mono scripting backend defaults to
            // TLS 1.0 / SSL 3.0, which most HTTPS servers no longer accept.
            // Explicitly enable TLS 1.2 to ensure connectivity across all Unity versions.
            // Unity 2022+ already defaults to TLS 1.2+, but the |= is additive and harmless.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static Yodo1Net GetInstance()
        {
            if (instance == null)
            {
                instance = new Yodo1Net();
            }
            return instance;
        }

        public Yodo1AppInfo GetAppInfoByAppKey(string appKey)
        {
            string url = BASE_URL + "/setup/" + appKey;
            string response = HttpGet(url);
            return Yodo1AppInfo.Parse(response);
        }

        public Yodo1AppInfo GetAppInfoByBundleID(string platform, string bundleId)
        {
            string url = BASE_URL + "/get-app-info-by-bundle-id";
            long unixTimeMs = (DateTime.UtcNow.Ticks - 621355968000000000L) / 10000L;
            string timestamp = unixTimeMs.ToString();
            string sign = Md5(SECRET_KEY + timestamp + bundleId).ToLower();

            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                { "timestamp", timestamp },
                { "bundle_id", bundleId },
                { "plugin_version", Yodo1AdUtils.GetPluginVersion() },
                { "platform", platform },
                { "sign", sign }
            };

            string response = HttpPost(url, Yodo1JSON.Serialize(dic));
            return Yodo1AppInfo.Parse(response);
        }

        internal static HttpWebRequest CreateRequest(string url, string method)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Timeout = REQUEST_TIMEOUT_MS;
            request.ReadWriteTimeout = REQUEST_TIMEOUT_MS;
            return request;
        }

        internal static string HttpPost(string url, string json)
        {
            HttpWebRequest request = CreateRequest(url, "POST");
            request.ContentType = "application/json";

            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(json);
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        internal static string HttpGet(string url)
        {
            HttpWebRequest request = CreateRequest(url, "GET");

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static string Md5(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
