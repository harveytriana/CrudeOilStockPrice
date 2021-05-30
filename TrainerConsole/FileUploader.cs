﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TrainerConsole
{
    public class FileUploader
    {
        public static async Task<bool> UploadFile(string filePath, string uri = null, bool prompt = true)
        {
            if (prompt) {
                Console.WriteLine($"Uploading {Path.GetFileName(filePath)} ...");
            }

            if (uri == null) {// trajectory of the published in IIS
                uri = "https://localhost:44308/api/FileUploader";
            }
            try {
                using (var formContent = new MultipartFormDataContent()) {
                    var Content = new ByteArrayContent(File.ReadAllBytes(filePath));
                    Content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    formContent.Add(Content, "file", Path.GetFileName(filePath));
                    using (HttpClient client = new()) {
                        var response = await client.PostAsync(uri, formContent);
                    }
                }
                return true;
            }
            catch (Exception exception) {
                Console.WriteLine($"Exception:\n{exception.Message}");
            }
            return false;
        }
    }
}