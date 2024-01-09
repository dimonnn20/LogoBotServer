using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogoBotServer
{
    class Program
    {
        //public static string serialNumber = "LP10024084";
        public static string pattern = @"^lp100\d{5}$";
        static async Task Main()
        {
            while (true)
            {
                await Console.Out.WriteLineAsync("Please enter the option you want");
                await Console.Out.WriteLineAsync("1 - To enter mashine serial number like LP********");
                await Console.Out.WriteLineAsync("2 - To enter the name of file with some serial numbers like LP********");
                await Console.Out.WriteLineAsync("3 or exit - To quit");
                //await Console.Out.WriteLineAsync("Please enter the mashine serial number like LP******** or exit to quit");
                string input = Console.ReadLine().Trim().ToLower();
                if (input.Equals("exit") && input.Equals("3"))
                {
                    return;
                }
                if (input.Equals("1"))
                {
                    while (true)
                    {
                        await Console.Out.WriteLineAsync("Please enter the mashine serial number like LP********");
                        string input1 = Console.ReadLine().Trim().ToLower();
                        if (isNumberCorrect(input1))
                        {
                            await DownloadDocFromLine(input1);
                            break;
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync("Entered serial number is not correct");
                        }
                    }

                }
                if (input.Equals("2"))
                {
                    while (true)
                    {
                        await Console.Out.WriteLineAsync("Please enter the name of file in program folder like ***.txt");
                        string input2 = Console.ReadLine().Trim().ToLower();
                        if (input2.EndsWith(".txt"))
                        {
                            while (true)
                            {
                                await Console.Out.WriteLineAsync("Please choose digit of the option and press enter or type exit to quit:");
                                await Console.Out.WriteLineAsync("Press 1 - If you want only documentation file");
                                await Console.Out.WriteLineAsync("Press 2 - If you want the whole documentation");
                                string input2_1 = Console.ReadLine().Trim().ToLower();
                                if (input2_1.Equals("1"))
                                {
                                    await DownloadDocFromFile(input2, true);
                                    break;
                                }
                                else if (input2_1.Equals("2"))
                                {
                                    await DownloadDocFromFile(input2, false);
                                    break;
                                }
                                else if (input2_1.Equals("exit"))
                                {
                                    return;
                                }

                            }
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync("Entered name of file is not correct");
                        }
                    }
                }
            }
        }

        private static List<Uri> getLinkList(HtmlDocument htmlDocument, bool isOnlyDoc)
        {
            string baseUrl = "http://10.1.5.100/tempDMS";
            var linkList = new List<Uri>();
            var fileLinks = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            foreach (var link in fileLinks)
            {
                string relativeUrl = link.GetAttributeValue("href", "");
                Uri absoluteUri = new Uri(new Uri(baseUrl), relativeUrl);
                if (isOnlyDoc)
                {
                    if (absoluteUri.ToString().Contains("print_documentation") || absoluteUri.ToString().Contains("print-documentation"))
                    {
                        linkList.Add(absoluteUri);
                        return linkList;
                    }
                }
                else
                {
                    linkList.Add(absoluteUri);
                }
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

            //string downloadFolder = AppDomain.CurrentDomain.BaseDirectory;
            //string fileName = Path.Combine(downloadFolder, getFileNameFromString(fileUrl.ToString()));
            //using (HttpClient fileClient = new HttpClient())
            //{
            //    byte[] fileContent = await fileClient.GetByteArrayAsync(fileUrl);
            //    File.WriteAllBytes(fileName, fileContent);
            //    Console.WriteLine($"File: {fileName} downloaded");
            //}


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
            }
            else
            {
                await Console.Out.WriteLineAsync("link list is zero");
            }
        }
        public static List<string> readSerialsFromFile(string name)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            List<string> numberList = new List<string>();
            try
            {
                Console.WriteLine("Start reading lines from file");
                string line;
                using (StreamReader reader = new StreamReader(path))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        string input = line.ToLower().Trim();
                        if (input.Length == 10 && Regex.IsMatch(input, pattern))
                        {
                            numberList.Add(input);
                        }
                    }

                }
                Console.WriteLine("Stop reading lines from file");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return numberList;
        }

        public async static Task DownloadDocFromFile(string fileName, bool isOnlyDocumentation)
        {
            // Начало скачивания документации из файла
            List<string> numberList = readSerialsFromFile(fileName);
            await Console.Out.WriteLineAsync("Start downloading files");
            foreach (string line in numberList)
            {
                List<Uri> linkList = new List<Uri>();
                HtmlDocument htmlDocument;
                try
                {
                    htmlDocument = await getHtmlResponse(line);
                    linkList = getLinkList(htmlDocument, isOnlyDocumentation);
                    await DownloadFromList(linkList);
                    await Console.Out.WriteLineAsync($"Success for SN: {line}");
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync("Sorry, something wrong");
                    await Console.Out.WriteLineAsync(ex.Message);

                }

            }
            await Console.Out.WriteLineAsync("Stop downloading files");
            Console.ReadLine();
        }
        public async static Task DownloadDocFromLine(string serialNumber)
        {
            List<Uri> linkList = new List<Uri>();
            HtmlDocument htmlDocument;
            htmlDocument = await getHtmlResponse(serialNumber);
            bool isOptionCorrect = false;
            while (!isOptionCorrect)
            {
                await Console.Out.WriteLineAsync("Please choose digit of the option and press enter or type exit to quit:");
                await Console.Out.WriteLineAsync("Press 1 - If you want only documentation file");
                await Console.Out.WriteLineAsync("Press 2 - If you want the whole documentation");
                string input2 = Console.ReadLine().Trim().ToLower();
                if (input2.Contains("exit")) { return; }
                await Console.Out.WriteLineAsync("Processing, please wait ...");
                int option = Convert.ToInt32(input2);
                if (option == 1)
                {
                    linkList = getLinkList(htmlDocument, true);
                    isOptionCorrect = true;
                }
                else if (option == 2)
                {
                    linkList = getLinkList(htmlDocument, false);
                    isOptionCorrect = true;
                }
                else
                {
                    await Console.Out.WriteLineAsync("Entered option is not supported");
                }
            }
            try
            {
                await DownloadFromList(linkList);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("Sorry, something wrong");
                await Console.Out.WriteLineAsync(ex.Message);

            }
        }
        public static bool isNumberCorrect(string number)
        {
            return (number.Length == 10 && Regex.IsMatch(number, pattern));
        }
    }
}
