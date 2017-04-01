using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace wxUploadMaterial
{
    public static class Utils
    {
        private static readonly IDictionary<string, string> ContentTypeExtensionsMapping = new Dictionary<string, string>
        {
            {".gif", "image/gif"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
            {".bmp", "application/x-bmp"},
            {".mp3", "audio/mp3"},
            {".wma", "audio/x-ms-wma"},
            {".wav", "audio/wav"},
            {".amr", "audio/amr"},
            {".mp4","video/mpeg4"}
        };

        public static string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if (String.IsNullOrEmpty(extension))
                return "";
            string contentType = ContentTypeExtensionsMapping[extension.ToLower()];
            return String.IsNullOrEmpty(contentType) ? "" : contentType;
        }

        public static string PostFile(string url, string file, string title, string introduction)
        {
            string contentType = GetContentType(file);
            if (String.IsNullOrEmpty(contentType))
                return "file type is not supported";
            bool isAudioVideo = (contentType.StartsWith("audio") || contentType.StartsWith("video"));
            string description = "";
            if (isAudioVideo)
            {
                if (String.IsNullOrEmpty(title))
                    return "title is null";
                if (String.IsNullOrEmpty(introduction))
                    return "introduction is null";
                description = String.Format("{{\"title\":\"{0}\", \"introduction\":\"{1}\"}}", title, introduction);
            }

            string boundary = "----" + DateTime.Now.Ticks.ToString("x");//微信要求boundary的值不可以存在双引号
            MultipartFormDataContent content = new MultipartFormDataContent(boundary);
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
            ByteArrayContent dataContent = new ByteArrayContent(File.ReadAllBytes(file));
            dataContent.Headers.Remove("Content-Type");
            dataContent.Headers.TryAddWithoutValidation("Content-Type", contentType);
            dataContent.Headers.Remove("Content-Disposition");
            dataContent.Headers.TryAddWithoutValidation("Content-Disposition", "form-data; name=\"media\"; filename=\"" + Path.GetFileName(file) + "\"");
            content.Add(dataContent, "media", "test.wav");
            if (!String.IsNullOrEmpty(description))
            {
                StringContent memoContent = new StringContent(description, Encoding.UTF8, "application/json");
                content.Add(memoContent, "description");
            }

            HttpClient client = new HttpClient();
            Task<HttpResponseMessage> task = client.PostAsync(url, content);
            return task.Result.Content.ReadAsStringAsync().Result;
        }
    }
}
