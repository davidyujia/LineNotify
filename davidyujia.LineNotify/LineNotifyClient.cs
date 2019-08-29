﻿using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace davidyujia.LineNotify
{
    public static class LineNotifyClient
    {
        private static readonly string TokenUrl = "https://notify-bot.line.me/oauth/token";
        private static readonly string NotifyUrl = "https://notify-api.line.me/api/notify";
        private static readonly string AuthUrl = "https://notify-bot.line.me/oauth/authorize?response_type=code&client_id={0}&redirect_uri={1}&scope=notify&state={2}";

        /// <summary>
        /// Get authorization url
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <param name="redirectUrl">Redirect URL(this URL will get "code" and "state")</param>
        /// <param name="state">your redirect URL will get this</param>
        /// <returns></returns>
        public static string GetAuthUrl(string clientId, string redirectUrl, string state = "")
        {
            return string.Format(AuthUrl, clientId, redirectUrl, state);
        }

        private static string ObjectToJson(object obj)
        {
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(obj.GetType());
                ser.WriteObject(ms, obj);
                var json = ms.ToArray();
                return Encoding.UTF8.GetString(json, 0, json.Length);
            }

        }

        private static T GetResult<T>(Stream stream) where T : class
        {
            var ser = new DataContractJsonSerializer(typeof(T));
            return ser.ReadObject(stream) as T;
        }

        private static void GetWebException(WebException webEx)
        {
            if (webEx.Response == null)
            {
                throw webEx;
            }

            using (var errorResponse = (HttpWebResponse)webEx.Response)
            {
                var apiResult = GetResult<ApiResult>(errorResponse.GetResponseStream());
                throw new Exception(apiResult.message);
            }
        }

        /// <summary>
        /// Get notify token
        /// </summary>
        /// <param name="code">Code</param>
        /// <param name="clientId">Client ID</param>
        /// <param name="clientSecret">Client Secret</param>
        /// <param name="redirectUri">Redirect URL</param>
        /// <returns></returns>
        public static async Task<string> GetTokenAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            string result = null;

            var request = (HttpWebRequest)HttpWebRequest.Create(TokenUrl);
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = "application/x-www-form-urlencoded";

            var postParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
            postParams.Add("grant_type", "authorization_code");
            postParams.Add("code", code);
            postParams.Add("redirect_uri", redirectUri);
            postParams.Add("client_id", clientId);
            postParams.Add("client_secret", clientSecret);

            var postData = Encoding.UTF8.GetBytes(postParams.ToString());
            request.ContentLength = postData.Length;

            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(postData, 0, postData.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    var apiResult = GetResult<AccessTokenResult>(response.GetResponseStream());
                    result = apiResult.access_token;
                }
            }
            catch (WebException webEx)
            {
                GetWebException(webEx);
            }

            return result;
        }

        /// <summary>
        /// Push message
        /// </summary>
        /// <param name="token">Notify token</param>
        /// <param name="message">Message</param>
        /// <returns></returns>
        public static async Task PushAsync(string token, string message)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(NotifyUrl);
            request.Method = WebRequestMethods.Http.Post;
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.ContentType = "application/x-www-form-urlencoded";

            var postParams = System.Web.HttpUtility.ParseQueryString(string.Empty);
            postParams.Add("message", message);

            var postData = Encoding.UTF8.GetBytes(postParams.ToString());
            request.ContentLength = postData.Length;


            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(postData, 0, postData.Length);
            }

            try
            {
                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                }
            }
            catch (WebException webEx)
            {
                GetWebException(webEx);
            }
        }

        [DataContract]
        private class ApiResult
        {
            [DataMember]
            internal string status { get; set; }
            [DataMember]
            internal string message { get; set; }
        }

        [DataContract]
        private class AccessTokenResult : ApiResult
        {
            [DataMember]
            internal string access_token { get; set; }
        }

    }
}
