﻿using HtmlAgilityPack;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CustomSpectreConsole
{
    public static class WebSearch
    {
        public static List<string> GetGoogleResults(string query, string searchKey)
        {
            Uri url = new Uri("http://www.google.com/search?q=" + query);
            ScrapingBrowser browser = new ScrapingBrowser();
            WebPage google = browser.NavigateToPage(url);

            HtmlNodeCollection nodes = google.Html.SelectNodes(string.Format("//*[contains(text(),'{0}') and name() != 'script']", searchKey));

            return nodes != null ? nodes.Select(x => x.InnerText).ToList() : new List<string>();
        }

        public static List<string> GetResults(string url, string xPath)
        {
            HtmlWeb web = new HtmlWeb();
            web.UserAgent = "user-agent=Mozilla/5.0" +
                            " (Windows NT 10.0; Win64; x64)" +
                            " AppleWebKit/537.36 (KHTML, like Gecko)" +
                            " Chrome/74.0.3729.169 Safari/537.36";

            var htmlDoc = web.Load(url);

            HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes(xPath);

            return nodes != null ? nodes.Select(x => x.InnerText).ToList() : new List<string>();
        }
    }
}
