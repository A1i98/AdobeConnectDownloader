﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AdobeConnectDownloader.Model;

namespace AdobeConnectDownloader.Application
{

    public delegate void PercentageChange(int percent, double byteRecive, double totalByte);
    public delegate void DownloadFileComplited(object? sender, System.ComponentModel.AsyncCompletedEventArgs e);

    public class WebManager
    {

        private WebClient webClient { get; set; } = null;

        public event PercentageChange PercentageChange;
        public event DownloadFileComplited DownloadFileComplited;
        public static VideoFileName GetDownloadUrl(string url)
        {
            var linkSplit = url.Split('/');
            string id = linkSplit[3];

            return new VideoFileName()
            {
                FileId = id,
                Url = $"{linkSplit[0]}//{linkSplit[2]}/{id}/output/{id}.zip?download=zip"
            };
        }

        public static Cookie? GetSessionCookieFrom(string url)
        {
            string sessionStr = "?session=";
            string sessionCookie = String.Empty;

            if (url.Contains(sessionStr))
            {
                int sessionIndex = url.IndexOf(sessionStr) + sessionStr.Length;
                if (url.IndexOf('&') == -1)
                    sessionCookie = url.Substring(sessionIndex);
                else
                    sessionCookie = url.Substring(sessionIndex, url.IndexOf('&') - sessionIndex);
            }

            if (sessionCookie == null)
                return null;
            else
            {
                string domain = url.Split('/')[2];
                return new Cookie("BREEZESESSION", sessionCookie, "/", domain);
            }
        }

        public async Task<bool> DownloadFile(string url, string fileAddress, Cookie cookie)
        {
            WebClient client = new WebClient();
            string CookiesStr = $"{cookie.Name}={cookie.Value}";

            client.Headers.Add(HttpRequestHeader.Cookie, CookiesStr);

            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            webClient = client;

            try
            {
                await client.DownloadFileTaskAsync(new Uri(url), fileAddress);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public void CancelDownload()
        {
            if (webClient != null)
                webClient.CancelAsync();
        }

        private void Client_DownloadFileCompleted(object? sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            DownloadFileComplited?.Invoke(sender, e);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString()) / (1024 * 1024);
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString()) / (1024 * 1024);
            int percentage = (int)(bytesIn / totalBytes * 100);
            PercentageChange?.Invoke(percentage, bytesIn, totalBytes);
        }

        public static List<Cookie> GetCookieForm(string url, Cookie cookie)
        {
            HttpWebRequest webRequest = WebRequest.CreateHttp(url);
            webRequest.Method = "HEAD";
            webRequest.CookieContainer = new CookieContainer();
            webRequest.CookieContainer.Add(cookie);

            List<Cookie> cookies = new List<Cookie>();

            var respose = (HttpWebResponse)webRequest.GetResponse();

            foreach (var x in respose.Cookies)
            {
                cookies.Add((Cookie)x);
            }

            return cookies;
        }

        public static string GetRequestUrl(string url, string session)
        {
            string originalURL = url.Split('?')[0];
            return $"{originalURL}?session={session}&proto=true";
        }

        public static bool IsUrlWrong(string url, List<Cookie> cookies)
        {
            HttpWebRequest webRequest = WebRequest.CreateHttp(url);
            webRequest.Method = "HEAD";
            webRequest.CookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
            {
                webRequest.CookieContainer.Add(cookie);
            }
            var respose = (HttpWebResponse)webRequest.GetResponse();

            if (respose.ContentType != "application/zip")
                return true;
            else
                return false;
        }
    }
}