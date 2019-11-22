using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.Storage.Blob;

namespace QueueConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("apsettings.json");

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            CloudStorageAccount account = CloudStorageAccount.Parse(config["connectionString"]);
            CloudQueueClient client = account.CreateCloudQueueClient();

            CloudQueue queue = client.GetQueueReference("filaprocesos");

            CloudQueueMessage peekMessage = queue.PeekMessage();

            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("operationscontainer");
            container.CreateIfNotExists();

            foreach (var item in queue.GetMessages(20, TimeSpan.FromSeconds(100)))
            {
                string filePath = string.Format(@"log_{0}.txt",item.Id);
                TextWriter tempFile = File.CreateText(filePath);
                var message = queue.GetMessage().AsString;
                tempFile.WriteLine(message);
                Console.WriteLine("Archivo creado");
                tempFile.Close();
                using (var fileStream = System.IO.File.OpenRead(filePath))
                {
                    CloudBlockBlob myBlob = container.GetBlockBlobReference(string.Format(@"log_{0}.txt",item.Id));
                    myBlob.UploadFromStream(fileStream);
                    Console.WriteLine("Blob creado");
                }
                queue.DeleteMessage(item);
            }
        }
    }
}
