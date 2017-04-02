using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WeiXin.Material
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

        /// <summary>
        /// 和微信错误信息的格式保持一致
        /// </summary>
        public static string GetErrorMessage(string message)
        {
            return String.Format("{{\"errcode\":\"101\", \"errmsg\":\"{0}\"}}", message);
        }

        public static HttpContent NewFileContent(byte[] contentData, string fileName, string contentType)
        {
            ByteArrayContent content = new ByteArrayContent(contentData);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
            content.Headers.ContentDisposition.Name = "\"media\"";
            content.Headers.ContentDisposition.FileName = $"\"{fileName}\"";
            return content;
        }

        public static HttpContent NewFileContent(string filePath)
        {
            string fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(filePath);
            string contentType = GetContentType(filePath);
            return NewFileContent(File.ReadAllBytes(filePath), fileName.ToLower(), contentType);
        }

        public static HttpContent NewDescription(string title, string introduction)
        {
            string description = String.Format("{{\"title\":\"{0}\", \"introduction\":\"{1}\"}}", title, introduction);
            StringContent content = new StringContent(description, Encoding.UTF8, "application/json");
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
            content.Headers.ContentDisposition.Name = "description";
            return content;
        }

        /// <summary>
        /// PostFile(url, NewFileContent(@"d:\test.wav"), NewDescription("Notify", "Bill Create Notify"));
        /// </summary>
        public static string PostFile(string url, HttpContent fileContent, HttpContent descriptionContent = null)
        {
            string contentType = fileContent.Headers.ContentType.MediaType;
            if (String.IsNullOrEmpty(contentType))
                throw new Exception(GetErrorMessage("file type is not supported"));

            bool isAudioVideo = (contentType.StartsWith("audio") || contentType.StartsWith("video"));
            if (isAudioVideo)
            {
                if (descriptionContent == null)
                    throw new Exception(GetErrorMessage("description is null"));
            }

            string boundary = "----" + DateTime.Now.Ticks.ToString("x");//微信要求boundary的值不可以存在双引号
            MultipartFormDataContent content = new MultipartFormDataContent(boundary);
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
            content.Add(fileContent);
            if (isAudioVideo)
                content.Add(descriptionContent);

            HttpClient client = new HttpClient();
            Task<HttpResponseMessage> task = client.PostAsync(url, content);
            return task.Result.Content.ReadAsStringAsync().Result;
        }
    }
}
