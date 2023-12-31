﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogoBotServer
{
    class Program
    {
        public static string serialNumber = "LP10024084";
        public static string pattern = @"^lp100\d{5}$";
        static async Task Main()
        {

            while (true)
            {

                await Console.Out.WriteLineAsync("Please enter the mashine serial number like LP******** or exit to quit");
                string input = Console.ReadLine().Trim().ToLower();
                if (input.Contains("exit"))
                {
                    return;
                }
                if (input.Length == 10 && Regex.IsMatch(input, pattern))
                {
                    await Console.Out.WriteLineAsync("Processing ...");
                    List<Uri> linkList = new List<Uri>();
                    HtmlDocument htmlDocument;
                    try
                    {
                        htmlDocument = await getHtmlResponse(input);
                        bool isOptionCorrect = false;
                        while (!isOptionCorrect)
                        {
                            await Console.Out.WriteLineAsync("Please choose digit of the option and press enter or type exit to quit:");
                            await Console.Out.WriteLineAsync("Press 1 - If you want only documentation file");
                            await Console.Out.WriteLineAsync("Press 2 - If you want the whole documentation");
                            string input2 = Console.ReadLine().Trim().ToLower();
                            if (input2.Contains("exit")) { return; }
                            int option = Convert.ToInt32(input2);
                            if (option == 1)
                            {
                                linkList = getLink(htmlDocument);
                                isOptionCorrect = true;
                            }
                            else if (option == 2)
                            {
                                linkList = getLinkList(htmlDocument);
                                isOptionCorrect = true;
                            }
                            else
                            {
                                await Console.Out.WriteLineAsync("Entered option is not supported");
                            }


                        }
                        await DownloadFromList(linkList);

                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync("Sorry, something wrong");
                        await Console.Out.WriteLineAsync(ex.Message);

                    }

                }
                else
                {
                    await Console.Out.WriteLineAsync("Entered serial number is not correct, please try again or exit");
                }

            }

        }

        private static List<Uri> getLink(HtmlDocument htmlDocument)
        {
            string baseUrl = "http://10.1.5.100/tempDMS";
            var linkList = new List<Uri>();
            var fileLinks = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            foreach (var link in fileLinks)
            {
                string relativeUrl = link.GetAttributeValue("href", "");
                Uri absoluteUri = new Uri(new Uri(baseUrl), relativeUrl);
                if (absoluteUri.ToString().Contains("print_documentation") || absoluteUri.ToString().Contains("print-documentation"))
                { linkList.Add(absoluteUri); }
            }
            return linkList;
        }

        private static List<Uri> getLinkList(HtmlDocument htmlDocument)
        {
            string baseUrl = "http://10.1.5.100/tempDMS";
            var linkList = new List<Uri>();
            var fileLinks = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            foreach (var link in fileLinks)
            {
                string relativeUrl = link.GetAttributeValue("href", "");
                Uri absoluteUri = new Uri(new Uri(baseUrl), relativeUrl);
                linkList.Add(absoluteUri);

            }
            return linkList;
        }

        static async Task<HtmlDocument> getHtmlResponse(string serialNumber)
        {

            string url = "http://10.1.5.100/tempDMS/index.php";
            using (HttpClient client = new HttpClient())
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("vmnumber", ""),
                new KeyValuePair<string, string>("serialnumber", serialNumber),
                new KeyValuePair<string, string>("Start", "Suche"),

            });
                HttpResponseMessage response = await client.PostAsync(url, formContent);
                string responseBody = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(responseBody);
                return htmlDoc;
            }

        }

        static async Task DownloadFileAsync(string fileUrl)
        {
            string downloadFolder = AppDomain.CurrentDomain.BaseDirectory + getFolderName(fileUrl.ToString());
            string fileName = Path.Combine(downloadFolder, getFileNameFromString(fileUrl.ToString()));
            Directory.CreateDirectory(downloadFolder);
            using (HttpClient fileClient = new HttpClient())
            {
                byte[] fileContent = await fileClient.GetByteArrayAsync(fileUrl);
                File.WriteAllBytes(fileName, fileContent);
                Console.WriteLine($"File: {fileName} downloaded");
            }
        }
        static async Task WriteToFile(List<string> lines)
        {
            string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt";
            string pathToSave = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            using (FileStream stream = new FileStream(pathToSave, FileMode.OpenOrCreate))
            {
                foreach (string line in lines)
                {
                    await stream.WriteAsync(Encoding.Default.GetBytes(line), 0, Encoding.Default.GetByteCount(line));
                    await stream.WriteAsync(Encoding.Default.GetBytes("\n"), 0, 1);
                }
            }
            await Console.Out.WriteLineAsync($"Report is successfuly saved to {pathToSave}");
        }
        static Uri CorrectUrl(Uri originalUrl)
        {
            // Извлекаем части URL
            string pathAndQuery = originalUrl.PathAndQuery;

            // Добавляем недостающий путь
            string correctedPath = "/tempDMS" + pathAndQuery;

            // Собираем корректированный URL
            string correctedUrl = originalUrl.GetLeftPart(UriPartial.Authority) + correctedPath;

            return new Uri(correctedUrl);
        }

        static string getFileNameFromString(string str)
        {
            string startString = "FILE=";
            string endString = "&TYPE";
            return str.Substring(str.IndexOf(startString) + startString.Length, (str.IndexOf(endString) - str.IndexOf(startString) - endString.Length));
        }

        static string getFolderName(string str)
        {
            string startPoint = "Seriennummern/";
            string endPoint = "&FILE";
            return str.Substring(str.IndexOf(startPoint) + startPoint.Length, str.IndexOf(endPoint) - (str.IndexOf(startPoint) + startPoint.Length)).Replace('/', '\\');
        }
        static async Task DownloadFromList(List<Uri> linkList)
        {

            if (linkList.Count > 0)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                List<string> strList = new List<string>();
                foreach (var item in linkList)
                {
                    strList.Add(CorrectUrl(item).ToString());
                }

                foreach (var item in strList)
                {
                    try
                    {
                        await DownloadFileAsync(item);
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync(ex.Message);
                    }
                }
                sw.Stop();
                await Console.Out.WriteLineAsync($"Completed for {sw.Elapsed} s");
                Console.ReadLine();

            }
            else
            {
                await Console.Out.WriteLineAsync("link list is zero");
            }
        }
    }
}
