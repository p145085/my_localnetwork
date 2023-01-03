using System.Collections.Generic;
using System.Net;
using my_localnetwork;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using System.Net.NetworkInformation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace my_localnetwork
{
    internal class Program
    {
        internal static async Task Initialize(WebScraperContext context)
        {
            context.Database.Migrate();
        }

        static void Main(string[] args)
        {
            // Configure the list of IP addresses to scrape
            List<string> ips_list = new List<string>();
            string baseip = "192.168.1.";
            for (int i = 0; i < 256; i++)
            {
                ips_list.Add(baseip + i);
            }
            // Set the directory where the text files will be saved
            string saveDirectory = "D:\\emiltempscraping";
            // Use Entity Framework to connect to the database and retrieve the data
            using (var db = new WebScraperContext())
            {
                db.Database.EnsureCreated();
                foreach (string ip in ips_list)
                {
                    // Retrieve the web page data
                    string data = GetWebPageData(ip);
                    // Save the data to a text file
                    if (data != null)
                    {
                        SaveToTextFile(data, saveDirectory, ip);
                        // Use Entity Framework to add a record to the database
                        db.WebPages.Add(new WebPage { IP = ip, Data = data });
                    }
                }
                // Save the changes to the database
                db.SaveChanges();
            }
            static string GetWebPageData(string ip)
            {
                // Use the WebClient class to retrieve the data
                using (WebClient client = new WebClient())
                {
                    // Check if the IP address is pingable
                    using (Ping ping = new Ping())
                    {
                        try
                        {
                            string url = "http://" + ip + ":80";
                            PingReply reply = ping.Send(ip, 5000);
                            if (reply.Status == IPStatus.Success)
                            {
                                // IP address is pingable, so download the data
                                return client.DownloadString("http://" + ip + ":80");
                            }
                            else
                            {
                                // IP address is not pingable
                                return null;
                            }
                        }
                        catch (PingException ex)
                        {
                            return ex.Message;
                        }
                        catch (WebException wex) // 28
                        {
                            return wex.Message;
                        }
                    }
                }
            }
            static void SaveToTextFile(string data, string saveDirectory, string ip)
            {
                // Create the file path
                string filePath = Path.Combine(saveDirectory, ip + ".txt");

                // Write the data to the file
                File.WriteAllText(filePath, data);
            }
        }

        

        // Entity Framework database context
        public class WebScraperContext : DbContext
        {
            public DbSet<WebPage> WebPages { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                // Configure the database connection string here
                optionsBuilder.UseSqlServer(@"Server=(localdb)\\MSSQLLocalDB;Database=my_localnetwork;Trusted_Connection=True;");
            }
        }
        // Entity Framework model for the WebPage table
        public class WebPage
        {
            public int ID { get; set; }
            public string? IP { get; set; }
            public string? Data { get; set; }
            public DateTime WhenSearched { get; set; } = DateTime.Now;
        }
    }
}