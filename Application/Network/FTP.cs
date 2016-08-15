using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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