using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Common;
using Common.DTO;
using Microsoft.Synchronization;

namespace SyncService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SyncService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select SyncService.svc or SyncService.svc.cs at the Solution Explorer and start debugging.
    [System.ServiceModel.ServiceBehavior()]
    public class SyncService : ISyncService
    {
        private string RemoteDirectoryPath = ConfigurationManager.AppSettings["RemoteDirectoryPath"];

        public SyncKnowledge GetCurrentSyncKnowledge()
        {
            using (SyncDetails sync = new LocalSyncDetails(RemoteDirectoryPath, true))
            {
                return sync.SyncKnowledge;
            }
        }
        public bool SaveSyncSession(LocalSyncDetails localSync)
        {
            localSync.Save(RemoteDirectoryPath);

            return true;
        }

        public void UploadFile(RemoteFileInfo request)
        {
            //must be temp folder in server 
            string workingPath = GetPathToWorkWith();
            FileInfo fi = new FileInfo(Path.Combine(workingPath, request.Metadata.Uri));

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            fi.Delete();

            int chunkSize = 2048;
            byte[] buffer = new byte[chunkSize];

            using (FileStream writeStream = new FileStream(fi.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                do
                {
                    int bytesRead = request.FileByteStream.Read(buffer, 0, chunkSize);
                    if (bytesRead == 0) break;

                    writeStream.Write(buffer, 0, bytesRead);
                } while (true);
            }
            GrantAccess(fi.FullName);
            if (workingPath != RemoteDirectoryPath )//useTemp Setting in web.config is true
                try
                {
                    fi.MoveTo(Path.Combine(RemoteDirectoryPath, fi.Name));
                }
                catch (Exception exception)
                {
                    fi.Delete();
                    Console.WriteLine(exception);
                }
        }

        public void DeleteFile(SyncId itemID, string itemUri)
        {

            if (itemUri!="")
            {
                File.Delete(Path.Combine(RemoteDirectoryPath, itemUri));
            }
        }

        public void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgotten)
        {
            using (SyncDetails sync = new LocalSyncDetails(RemoteDirectoryPath, false))
            {
                sync.StoreKnowledgeForScope(knowledge, forgotten);
            }

        }

        public byte[] GetChanges(uint batchSize, SyncKnowledge destinationKnowledge, LocalSyncDetails syncDetails)
        {
            ChangeBatchTransfer changeBatch = new ChangeBatchTransfer();
            object dataRetriver = new object();
            changeBatch.ChangeBatch = syncDetails.GetChangeBatch(RemoteDirectoryPath, batchSize, destinationKnowledge, out dataRetriver);
            changeBatch.ChangeDataRetriever = dataRetriver;

            return changeBatch.ObjectToByteArray();
        }

        public LocalSyncDetails LoadSyncSession()
        {
            return new LocalSyncDetails(RemoteDirectoryPath, true);
        }


        public Stream DownloadFile(string file)
        {
            return new FileStream(Path.Combine(RemoteDirectoryPath, file), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        #region TestClientDemo

        public void CreateFileTest()
        {
            new Common.TestClientDemo().CreateNewFile(RemoteDirectoryPath);
        }
        public void DeleteTestFile(string file)
        {
            new Common.TestClientDemo().DeleteFile(Path.Combine(RemoteDirectoryPath, file));
        }
        public string DownloadSingleFile(string filepath)
        {
            using (StreamReader sr = new StreamReader(Path.Combine(RemoteDirectoryPath, filepath)))
            {
                return sr.ReadToEnd();
            }
        }

        public void EditServerTextFile(string file, string text)
        {
            new TestClientDemo().EditTextFile(Path.Combine(RemoteDirectoryPath, file), text);

        }

        public List<FileInfo> GetServerFileInfo()
        {
            return new Common.TestClientDemo().GetFilesInfo(RemoteDirectoryPath);
        }



        #endregion

        public static bool GrantAccess(string fullPath)
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();

                DirectoryInfo dInfo = new DirectoryInfo(fullPath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(id.User.AccountDomainSid, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);
                return true;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
            return false;
        }
        private string GetPathToWorkWith()
        {
            var useTemp = AppSettings.Get<bool>("UseTemp");
            string workingPath;
            if (useTemp)
                workingPath = AppSettings.Get<string>("TempDirectoryPath");
            else
                workingPath = RemoteDirectoryPath;
            return workingPath;
        }
    }

}
