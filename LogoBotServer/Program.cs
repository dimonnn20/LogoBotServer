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
        static async Task Main()
        {
            
            List <Uri> linkList = new List<Uri>();
            HtmlDocument htmlDocument =  await getHtmlResponse();
            linkList = getLinkList(htmlDocument);
            Uri exampleLink = linkList.First();
            foreach (var item in linkList)
            {
                await Console.Out.WriteLineAsync(item.ToString());
            }
            await DownloadFileAsync(exampleLink);
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

        static async Task <HtmlDocument> getHtmlResponse()
        {
           
            string url = "http://10.1.5.100/tempDMS/index.php";
            using (HttpClient client = new HttpClient())
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("vmnumber", ""),
                new KeyValuePair<string, string>("serialnumber", "LP10024084"),
                new KeyValuePair<string, string>("Start", "Suche"),

            });
                HttpResponseMessage response = await client.PostAsync(url, formContent);
                string responseBody = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml( responseBody );
                return htmlDoc;
            }
            
        }

        static async Task DownloadFileAsync(Uri fileUrl) 
        {
            string downloadFolder = AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.Combine(downloadFolder, "xxx");

            using (HttpClient fileClient = new HttpClient())
            {
                byte[] fileContent = await fileClient.GetByteArrayAsync(fileUrl);
                File.WriteAllBytes(fileName, fileContent);
                Console.WriteLine($"File: {fileName} downloaded");
            }
        }
    }
}
