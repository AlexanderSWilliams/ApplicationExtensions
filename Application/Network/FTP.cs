using System.IO;
using System.Net;

namespace Application.Network
{
    public static class FTP
    {
        public static void CopyFromServer(string ftpServerName, string userName, string passWord, string ftpFilePath, string destinationPath)
        {
            using (var request = new WebClient())
            {
                request.Credentials = new NetworkCredential(userName, passWord);
                var fileData = request.DownloadData("ftp://" + ftpServerName + ftpFilePath);

                using (var file = File.Create(destinationPath))
                {
                    file.Write(fileData, 0, fileData.Length);
                    file.Close();
                }
            }
        }
    }
}