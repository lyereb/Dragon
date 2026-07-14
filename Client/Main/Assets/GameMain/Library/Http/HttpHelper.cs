using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Dragon.Library
{
    /// <summary>
    /// HTTP 请求封装，基于 UnityWebRequest + UniTask。
    /// </summary>
    public static class HttpHelper
    {
        public const int DefaultTimeoutSeconds = 30;

        public sealed class HttpRequestOptions
        {
            public int TimeoutSeconds = DefaultTimeoutSeconds;
            public CancellationToken CancellationToken = default;
            public Dictionary<string, string> Headers;
        }

        public sealed class HttpResponse
        {
            public bool IsSuccess;
            public long StatusCode;
            public string Text;
            public byte[] Data;
            public string Error;
            public Dictionary<string, string> Headers;

            public bool TryGetHeader(string key, out string value)
            {
                if (Headers == null || string.IsNullOrEmpty(key))
                {
                    value = null;
                    return false;
                }

                return Headers.TryGetValue(key, out value);
            }
        }

        public static UniTask<HttpResponse> GetAsync(string url, HttpRequestOptions options = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            return SendAsync(request, options);
        }

        public static UniTask<HttpResponse> PostAsync(
            string url,
            byte[] body,
            string contentType = "application/octet-stream",
            HttpRequestOptions options = null)
        {
            return SendWithBodyAsync(UnityWebRequest.kHttpVerbPOST, url, body, contentType, options);
        }

        public static UniTask<HttpResponse> PostJsonAsync(
            string url,
            string json,
            HttpRequestOptions options = null)
        {
            byte[] body = string.IsNullOrEmpty(json)
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(json);
            return PostAsync(url, body, "application/json", options);
        }

        public static UniTask<HttpResponse> PostFormAsync(string url, WWWForm form, HttpRequestOptions options = null)
        {
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form));
            }

            UnityWebRequest request = UnityWebRequest.Post(url, form);
            return SendAsync(request, options);
        }

        /// <summary>
        /// POST application/x-www-form-urlencoded form fields.
        /// </summary>
        public static UniTask<HttpResponse> PostUrlEncodedFormAsync(
            string url,
            Dictionary<string, string> fields,
            HttpRequestOptions options = null)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var bodyBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> field in fields)
            {
                if (bodyBuilder.Length > 0)
                {
                    bodyBuilder.Append('&');
                }

                bodyBuilder.Append(UnityWebRequest.EscapeURL(field.Key));
                bodyBuilder.Append('=');
                bodyBuilder.Append(UnityWebRequest.EscapeURL(field.Value ?? string.Empty));
            }

            byte[] body = Encoding.UTF8.GetBytes(bodyBuilder.ToString());
            return PostAsync(url, body, "application/x-www-form-urlencoded", options);
        }

        public static UniTask<HttpResponse> PutAsync(
            string url,
            byte[] body,
            string contentType = "application/octet-stream",
            HttpRequestOptions options = null)
        {
            return SendWithBodyAsync(UnityWebRequest.kHttpVerbPUT, url, body, contentType, options);
        }

        public static UniTask<HttpResponse> PutTextAsync(
            string url,
            string text,
            string contentType = "text/plain; charset=utf-8",
            HttpRequestOptions options = null)
        {
            byte[] body = string.IsNullOrEmpty(text)
                ? Array.Empty<byte>()
                : Encoding.UTF8.GetBytes(text);
            return PutAsync(url, body, contentType, options);
        }

        public static UniTask<HttpResponse> DeleteAsync(string url, HttpRequestOptions options = null)
        {
            UnityWebRequest request = UnityWebRequest.Delete(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            return SendAsync(request, options);
        }

        public static UniTask<HttpResponse> HeadAsync(string url, HttpRequestOptions options = null)
        {
            UnityWebRequest request = UnityWebRequest.Head(url);
            return SendAsync(request, options);
        }

        public static UniTask<HttpResponse> SendAsync(
            string method,
            string url,
            byte[] body = null,
            string contentType = null,
            HttpRequestOptions options = null)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("HTTP method is required.", nameof(method));
            }

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL is required.", nameof(url));
            }

            UnityWebRequest request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (body != null && body.Length > 0)
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                if (!string.IsNullOrEmpty(contentType))
                {
                    request.SetRequestHeader("Content-Type", contentType);
                }
            }

            return SendAsync(request, options);
        }

        public static async UniTask<byte[]> DownloadBytesAsync(string url, HttpRequestOptions options = null)
        {
            HttpResponse response = await GetAsync(url, options);
            if (!response.IsSuccess)
            {
                Debug.LogError($"[HttpHelper] DownloadBytes failed: {response.Error}, url={url}");
                return null;
            }

            return response.Data;
        }

        public static async UniTask<Texture2D> DownloadTextureAsync(string url, HttpRequestOptions options = null)
        {
            options ??= new HttpRequestOptions();
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            ApplyOptions(request, options);

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: options.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[HttpHelper] DownloadTexture cancelled: url={url}");
                return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[HttpHelper] DownloadTexture failed: {request.error}, url={url}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        public static async UniTask<bool> DownloadFileAsync(
            string url,
            string savePath,
            HttpRequestOptions options = null)
        {
            if (string.IsNullOrEmpty(savePath))
            {
                throw new ArgumentException("Save path is required.", nameof(savePath));
            }

            options ??= new HttpRequestOptions();
            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(savePath);
            ApplyOptions(request, options);

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: options.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[HttpHelper] DownloadFile cancelled: url={url}");
                return false;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[HttpHelper] DownloadFile failed: {request.error}, url={url}, path={savePath}");
                return false;
            }

            return true;
        }

        private static UniTask<HttpResponse> SendWithBodyAsync(
            string method,
            string url,
            byte[] body,
            string contentType,
            HttpRequestOptions options)
        {
            body ??= Array.Empty<byte>();
            UnityWebRequest request = new UnityWebRequest(url, method);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(contentType))
            {
                request.SetRequestHeader("Content-Type", contentType);
            }

            return SendAsync(request, options);
        }

        private static async UniTask<HttpResponse> SendAsync(UnityWebRequest request, HttpRequestOptions options)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            options ??= new HttpRequestOptions();
            ApplyOptions(request, options);

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: options.CancellationToken);
                return BuildResponse(request);
            }
            catch (OperationCanceledException)
            {
                return new HttpResponse
                {
                    IsSuccess = false,
                    StatusCode = request.responseCode,
                    Error = "Request cancelled."
                };
            }
            finally
            {
                request.Dispose();
            }
        }

        private static void ApplyOptions(UnityWebRequest request, HttpRequestOptions options)
        {
            if (options.TimeoutSeconds > 0)
            {
                request.timeout = options.TimeoutSeconds;
            }

            if (options.Headers == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> header in options.Headers)
            {
                if (string.IsNullOrEmpty(header.Key))
                {
                    continue;
                }

                request.SetRequestHeader(header.Key, header.Value ?? string.Empty);
            }
        }

        private static HttpResponse BuildResponse(UnityWebRequest request)
        {
            HttpResponse response = new HttpResponse
            {
                StatusCode = request.responseCode,
                Headers = request.GetResponseHeaders(),
                IsSuccess = request.result == UnityWebRequest.Result.Success,
                Error = request.error
            };

            DownloadHandler handler = request.downloadHandler;
            if (handler is DownloadHandlerBuffer bufferHandler)
            {
                response.Data = bufferHandler.data;
                response.Text = bufferHandler.text;
            }

            return response;
        }
    }
}
