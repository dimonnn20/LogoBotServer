using System;
using System.Collections.Generic;
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
           await getToken();
            Console.ReadLine();

        }



        static async Task getToken()
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
                // Печать статуса ответа
                Console.WriteLine($"Status code: {response.StatusCode}");
                // Печать тела ответа
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Body: {responseBody}");
                
            }
        }
    }
}
