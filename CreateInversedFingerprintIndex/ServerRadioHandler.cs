using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CreateInversedFingerprintIndex
{
    class ServerRadioHandler
    {
        //KnownBug - Kills match audio when lucene is finished
        private HttpClient clientMUR02;
        private HttpClient clientMUR03;
        private List<HttpClient> clients;

        public ServerRadioHandler()
        {
            CreateAndInstanzialiseClient();
            Task.Run(() => MoveFiles()).Wait();
        }

        private async Task MoveFiles()
        {
            //foreach known server
            foreach (HttpClient client in clients)
            {
                var radioIDs = await GetAllRadios(client);

                await CopyLuceneToServer(@"C:/LuceneCopy", client);

                await StopRadios(client);

                await DeleteAndRenameOnServer(@"C:/LuceneCopy",client);
                //start all previous on going radios
                await StartAllRadios(client, radioIDs);
            }
        }

        public async Task DeleteAndRenameOnServer(string serverLuceneCopyPath, HttpClient client)
        {
            string serverLucenePathJSON = $"'{serverLuceneCopyPath}'";
            var response = await client.PostAsync("/api/Audio/deleteandrenamelucene", new StringContent(serverLucenePathJSON, Encoding.UTF8, "application/json"));
            var test = response.Content;
            response.EnsureSuccessStatusCode();
            Console.WriteLine("Lucene has been updated");
        }

        private async Task CopyLuceneToServer(string serverLucenePath, HttpClient client)
        {
            string serverLucenePathJSON = $"'{serverLucenePath}'";
            var response = await client.PostAsync("/api/Audio/movelucenetoserver", new StringContent(serverLucenePathJSON, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            Console.WriteLine("Lucene has been copied to " + serverLucenePath);
        }


        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void ClearFolder(string folderName)
        {
            DirectoryInfo dir = new DirectoryInfo(folderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.IsReadOnly = false;
                fi.Delete();
            } //foreach

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            } //foreach
        }

        public async Task StartAllRadios(HttpClient client, List<string> radioIDs)
        {
            foreach (string id in radioIDs)
                await StartRadio(id, client);
        }


        private void CreateAndInstanzialiseClient()
        {
            clientMUR02 = new HttpClient
            {
                BaseAddress = new Uri("http://ITUMUR02:8080")
            };
            //clientMUR03 = new HttpClient
            //{
            //    BaseAddress = new Uri("http://ITUMUR03:8080")
            //};
            clients = new List<HttpClient>();
            clients.Add(clientMUR02);
           // clients.Add(clientMUR03);
        }

        private async Task<List<string>> GetAllRadios(HttpClient client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/Audio/radio");
            string responseBody = await response.Content.ReadAsStringAsync();
            List<string> radios = responseBody.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            return radios;
        }

        private async Task<string> StartRadio(string radioID, HttpClient client)
        {
            string radioIDJSON = $"'{radioID}'";
            var response = await client.PostAsync("/api/Audio/startradio", new StringContent(radioIDJSON, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(radioID + " has been started");

            return radioID + " Radios has been started";
        }

        private async Task<string> StopRadio(string radioID, HttpClient client)
        {
            string radioIDJSON = $"'{radioID}'";
            var response = await client.PostAsync("/api/Audio/stopradio", new StringContent(radioIDJSON, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(radioID + " has been stopped");
            return radioID + " radio has been stopped";
        }

        private async Task StopRadios(HttpClient client)
        {
            var response = await client.GetAsync("/api/Audio/stopallradios");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
        }
    }
}
