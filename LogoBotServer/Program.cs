using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace LogoBotServer
{
    class Program
    {
        public static string serialNumber = "LP10024084";
        static async Task Main()
        {

            List<Uri> linkList = new List<Uri>();
            HtmlDocument htmlDocument = await getHtmlResponse(serialNumber);
            linkList = getLinkList(htmlDocument);
            Uri exampleLink = CorrectUrl(linkList.First());
            List<string> strList = new List<string>();
            foreach (var item in linkList)
            {
                strList.Add(CorrectUrl(item).ToString());
            }
            await WriteToFile(strList);
            foreach (var item in strList)
            {
                try
                { await DownloadFileAsync(item); }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                }

                
            }
            Console.ReadLine();

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
            string startPoint = "Seriennummern";
            string endPoint = "&FILE";
            return str.Substring(str.IndexOf(startPoint) + startPoint.Length, str.IndexOf(endPoint) - (str.IndexOf(startPoint) + startPoint.Length));
        }
    }
}
