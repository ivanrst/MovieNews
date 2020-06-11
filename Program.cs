using RestSharp;
using Json.Net;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Reflection;
using System.Net.Mail;
using System.Net;
using System.Configuration;
using System.ComponentModel;
using System.Threading;
using System.Linq;

namespace MovieNews
{
    class Program
    {
        static void Main(string[] args)
        {
            string email = "";
            string movieName;
            bool validEmail = false;
            // this is a free api key generated and sent by OMDB
            string apiKey = "4b355673";

            Console.Write("Enter your e-mail address: ");
            email = Console.ReadLine();
            validEmail = CheckEmail(email);

            while (!validEmail)
            {
                Console.Write("Enter a valid e-mail address: ");
                email = Console.ReadLine();
                validEmail = CheckEmail(email);
            }

            Console.Write("Enter a movie name: ");
            movieName = Console.ReadLine();

            List<Movie> movies = MakeRequest(apiKey, movieName);

            CreateCSV(movies);

            if (validEmail)
            {
                SendMail(email);
            }

        }

        private static bool CheckEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static List<Movie> MakeRequest(string apiKey, string movieName)
        {
            var client = new RestClient("http://www.omdbapi.com/");
            var request = new RestRequest("?apikey=" + apiKey + "&s=" + movieName, Method.GET);

            // The response from omdb only contains the first page and is in JSON format by default.
            var queryResult = client.Execute(request).Content;

            /* If you search for a real movie, the response will contain all the relevant movies. The response tag will be true.
             * If you enter some random characters, the response tag will be false. 
             * Here I check this tag. Since it is in the end of the message, I use substring to get the tag. 
             */
            var responseCheck = queryResult.Substring(queryResult.LastIndexOf(",") + 1);

            if (responseCheck.Contains("Response") && responseCheck.Contains("True"))
            {
                /*Our query result contains a list of movie object inside a search tag. 
                 *  I use substring to remove this tag and other unnecessary tags like total results and response at the end.
                 *  Then we get a neat string which only contains movie objects.
                 *  We de-serialise it and get a list of movie objects
                 */
                queryResult = queryResult.Substring(0, queryResult.LastIndexOf("]") + 1);
                queryResult = queryResult.Substring(queryResult.IndexOf("["));
                return JsonConvert.DeserializeObject<List<Movie>>(queryResult);
            }

            return null;

        }

        private static void CreateCSV(List<Movie> movies)
        {
            if (movies != null)
            {
                var csv = new StringBuilder();

                // the header
                var newLine = "Title;Year;Type";
                csv.AppendLine(newLine);

                foreach (var item in movies)
                {
                    newLine = string.Format("{0};{1};{2}", item.Title, item.Year, item.Type);
                    csv.AppendLine(newLine);
                }

                File.WriteAllText(Directory.GetCurrentDirectory() + @"\MyFile.csv", csv.ToString());
            }

        }

        private static void SendMail(string email)
        {
            if (File.Exists(Directory.GetCurrentDirectory() + @"\MyFile.csv"))
            {
                var smtpClient = new SmtpClient("smtp.gmail.com", 587);
                smtpClient.UseDefaultCredentials = false;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.EnableSsl = true;

                // these come from the app.config file
                smtpClient.Credentials = new NetworkCredential()
                {
                    UserName = ConfigurationManager.AppSettings.Get("username"),
                    Password = ConfigurationManager.AppSettings.Get("password")
                };

                MailAddress mailfrom = new MailAddress(ConfigurationManager.AppSettings.Get("username"), "MovieNews");
                MailAddress mailto = new MailAddress(email);
                MailMessage newmsg = new MailMessage(mailfrom, mailto);

                newmsg.Subject = "Movies search result";
                newmsg.Body = "Body(message) of email";

                // Attach the csv file. 
                Attachment att = new Attachment(Directory.GetCurrentDirectory() + @"\MyFile.csv");
                newmsg.Attachments.Add(att);

                smtpClient.Send(newmsg);

                att.Dispose();

                FileInfo file = new FileInfo(Directory.GetCurrentDirectory() + @"\MyFile.csv");

                file.Delete();

            }

        }

    }

    // I have create a movie class to de-serialise our JSON response 
    public class Movie
    {
        public string Title { get; set; }
        public string Year { get; set; }
        public string imdbID { get; set; }
        public string Type { get; set; }
        public string Poster { get; set; }
    }
}
