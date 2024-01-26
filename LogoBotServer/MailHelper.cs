using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogoBotServer
{

    public class MailHelper
    {
        private string fromAdress = "logopakeastbot@gmail.com";
        private string password = "vibg ppop yidy otic";
        private string host = "imap.gmail.com";



        //public void SendEmailMessage()
        //{
        //    try
        //    {
        //        MailMessage mailMessage = new MailMessage(fromAdress, toAdress);
        //        mailMessage.Subject = "My first message";
        //        mailMessage.Body = "Here is some regards";

        //        SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
        //        smtpClient.Port = 587;
        //        smtpClient.Credentials = new System.Net.NetworkCredential(fromAdress, password);
        //        smtpClient.EnableSsl = true;

        //        smtpClient.Send(mailMessage);
        //        Console.WriteLine("Email has sent successfuly");

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Exception: " + ex.ToString());
        //    }
        //}

        public void SendEmailFile(string toAdress,string pathToFile)
        {
            string pathToZipFile = GetPathToZipFile(pathToFile);
            string sub = Path.GetFileNameWithoutExtension(pathToZipFile);
            try
            {
                MailMessage mailMessage = new MailMessage(fromAdress, toAdress);
                mailMessage.Subject = sub;
                //mailMessage.Body = "Here is some regards";
                Attachment attachment = new Attachment(pathToZipFile);
                mailMessage.Attachments.Add(attachment);

                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
                smtpClient.Port = 587;
                smtpClient.Credentials = new System.Net.NetworkCredential(fromAdress, password);
                smtpClient.EnableSsl = true;

                smtpClient.Send(mailMessage);
                Console.WriteLine("Email has sent successfuly");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
        }

        private string GetPathToZipFile(string pathToFolder)
        {
            try
            {
                // Извлекаем имя папки из пути
                string folderName = new DirectoryInfo(pathToFolder).Name;

                // Формируем имя архива
                string zipFileName = $"{folderName}_{DateTime.Now:yyyyMMddHHmmss}.zip";
                string zipFilePath = Path.Combine(pathToFolder, zipFileName);

                // Создаем архив
                ZipFile.CreateFromDirectory(pathToFolder, zipFilePath);

                Console.WriteLine($"Archive created successfully: {zipFilePath}");

                // Возвращаем путь к созданному архиву
                return zipFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.ToString()}");
                return null;
            }
        }

        public async Task <Dictionary<string, string>> ReadEmails()
        {
            Dictionary<string,string> dictionaryOfSubjects = new Dictionary<string, string> ();

                Console.WriteLine("Starts reading emails");
                using (var client = new ImapClient())
                {
                Console.WriteLine("Try to connect");
                client.Connect(host, 993, true);

                    // Аутентификация
                    client.Authenticate(fromAdress, password);
                Console.WriteLine("Authentication success");
                // Открываем папку "Входящие"
                var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);

                    // Ищем все сообщения во входящих
                    var unreadMessages = inbox.Search(SearchQuery.NotSeen);
                Console.WriteLine("Looking for emails");
                if (unreadMessages.Count > 0)
                    {
                        foreach (var uniqueId in unreadMessages)
                        {
                            var message = inbox.GetMessage(uniqueId);

                            // Извлекаем информацию о сообщении
                            string sender = message.From.ToString();
                            string subject = message.Subject;
                            Console.WriteLine("Looking for email");
                            if (sender.Contains("dmakarau@logopakeast.pl"))
                            {
                                Console.WriteLine($"Email from {sender} with the subject: {subject.ToLower().Trim()}");
                                //dictionaryOfSubjects.Add(sender,subject.ToLower().Trim());
                            }


                            // Добавьте вашу логику обработки сообщения здесь

                            // Удаляем сообщение
                            inbox.AddFlags(uniqueId, MessageFlags.Deleted, true);
                            inbox.Expunge();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Inbox is empty");
                    }


                    client.Disconnect(true);
                }
                return dictionaryOfSubjects;
            
        }
    }
}
