using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using IPCSoftware.Shared.Models.ConfigModels;
using Microsoft.Extensions.Logging;

namespace IPCSoftware.CoreService.Services.CCD
{
    public class FtpClient
    {
       // private readonly ILogger<FtpClient> _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public FtpClient(CameraInterfaceModel camera )
        {
            //_logger = logger;
            _host = camera.IPAddress;
            _port = camera.Port;
            if (!camera.AnonymousLogin)
            {
                _username = camera.Username;
                _password = camera.Password;
            }
        }

        /// <summary>
        /// Downloads a file from the FTP server
        /// </summary>
        public async Task<bool> DownloadFileAsync(string remoteFilePath, string localFilePath)
        {
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/{remoteFilePath.TrimStart('/')}");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.DownloadFile);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var responseStream = response.GetResponseStream();
                using var fileStream = new FileStream(localFilePath, FileMode.Create);

                await responseStream.CopyToAsync(fileStream);

              //  _logger.LogInformation($"Downloaded: {remoteFilePath} -> {localFilePath} (Status: {response.StatusDescription})");
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Failed to download file: {remoteFilePath}");
                return false;
            }
        }
        
        /// <summary>
        /// Uploads a file to the FTP server
        /// </summary>
        public async Task<bool> UploadFileAsync(string localFilePath, string remoteFilePath)
        {
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/{remoteFilePath.TrimStart('/')}");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.UploadFile);

                using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                using var requestStream = await request.GetRequestStreamAsync();

                await fileStream.CopyToAsync(requestStream);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                //_logger.LogInformation($"Uploaded: {localFilePath} -> {remoteFilePath} (Status: {response.StatusDescription})");
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Failed to upload file: {localFilePath}");
                return false;
            }
        }

        /// <summary>
        /// Lists files and directories in the specified path
        /// </summary>
        public async Task<List<string>> ListDirectoryAsync(string remotePath = "/")
        {
            var items = new List<string>();
            try
            {
                string cleanRemotePath = "/" + remotePath.TrimStart('/').Trim(); // Handles " / " typo
                var uri = new Uri($"ftp://{_host}:{_port}{cleanRemotePath}");

                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.ListDirectory);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var responseStream = response.GetResponseStream();
                using var reader = new StreamReader(responseStream);

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    items.Add(line);
                }

               // _logger.LogInformation($"Listed {items.Count} items in directory: {remotePath}");
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Failed to list directory: {remotePath}");
            }
            return items;
        }




        /// <summary>
        /// Gets detailed file listing with sizes and timestamps
        /// </summary>
        public async Task<List<string>> ListDirectoryDetailsAsync(string remotePath = "/")
        {
            var items = new List<string>();
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/{remotePath.TrimStart('/')}");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.ListDirectoryDetails);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                using var responseStream = response.GetResponseStream();
                using var reader = new StreamReader(responseStream);

                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    items.Add(line);
                }

               // _logger.LogInformation($"Listed {items.Count} items with details in: {remotePath}");
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Failed to list directory details: {remotePath}");
            }
            return items;
        }

        /// <summary>
        /// Deletes a file from the FTP server
        /// </summary>
        public async Task<bool> DeleteFileAsync(string remoteFilePath)
        {
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/{remoteFilePath.TrimStart('/')}");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.DeleteFile);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
               //_logger.LogInformation($"Deleted file: {remoteFilePath} (Status: {response.StatusDescription})");
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Failed to delete file: {remoteFilePath}");
                return false;
            }
        }

        /// <summary>
        /// Creates a directory on the FTP server
        /// </summary>
        public async Task<bool> CreateDirectoryAsync(string remotePath)
        {
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/{remotePath.TrimStart('/')}");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.MakeDirectory);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
               // _logger.LogInformation($"Created directory: {remotePath} (Status: {response.StatusDescription})");
                return true;
            }
            catch (Exception ex)
            {
              ///  _logger.LogError(ex, $"Failed to create directory: {remotePath}");
                return false;
            }
        }

        /// <summary>
        /// Gets the size of a file
        /// </summary>
        public async Task<long> GetFileSizeAsync(string remoteFilePath)
        {
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/{remoteFilePath.TrimStart('/')}");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.GetFileSize);

                using var response = (FtpWebResponse)await request.GetResponseAsync();
                var size = response.ContentLength;
                //_logger.LogInformation($"File size for {remoteFilePath}: {size} bytes");
                return size;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Failed to get file size: {remoteFilePath}");
                return -1;
            }
        }

        /// <summary>
        /// Tests the connection to the FTP server
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var uri = new Uri($"ftp://{_host}:{_port}/");
                var request = CreateFtpRequest(uri, WebRequestMethods.Ftp.ListDirectory);
                request.Timeout = 5000; // 5 second timeout for connection test

                using var response = (FtpWebResponse)await request.GetResponseAsync();
               // _logger.LogInformation($"Connection successful to {_host}:{_port} (Status: {response.StatusDescription})");
                return true;
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, $"Connection failed to {_host}:{_port}");
                return false;
            }
        }

        private FtpWebRequest CreateFtpRequest(Uri uri, string method)
        {
            var request = (FtpWebRequest)WebRequest.Create(uri);
            request.Method = method;
            request.Credentials = new NetworkCredential(_username, _password);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = 30000; // 30 seconds
            return request;
        }
    }
}