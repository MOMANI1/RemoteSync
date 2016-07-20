using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Common;
using Microsoft.Synchronization;


namespace SyncFrameWork.Controllers
{
    public class LocalStore : KnowledgeSyncProvider, INotifyingChangeApplierTarget
    {
        private string folderPath;
        private SyncSessionContext currentSessionContext;
        private uint requestedBatchSize = 1;
        public LocalSyncDetails sync = null;
        MemoryConflictLog _memConflictLog;

        public LocalStore(string folderPath)
        {
            sync = new LocalSyncDetails(folderPath, true);
            this.folderPath = folderPath;
        }

        private string FolderPath
        {
            get { return folderPath; }
        }
        public uint RequestedBatchSize
        {
            get { return requestedBatchSize; }
            set { requestedBatchSize = value; }
        }

        public override void BeginSession(Microsoft.Synchronization.SyncProviderPosition providerPosition, SyncSessionContext syncSessionContext)
        {
            currentSessionContext = syncSessionContext;
            _memConflictLog = new MemoryConflictLog(IdFormats);
        }

        public override void EndSession(SyncSessionContext syncSessionContext)
        {
            sync.Save(ConfigurationManager.AppSettings["LocalAddress"]);
            System.Diagnostics.Debug.WriteLine("_____   Ending Session On LocalStore   ______" );
           
        }

        public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
        {
            return sync.GetChangeBatch(batchSize, destinationKnowledge, out changeDataRetriever);

        }

        public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval,out object changeDataRetriever)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
        {
            if (sync == null)
                throw new InvalidOperationException("Knowledge not yet loaded.");

            sync.SetLocalTickCount();

            batchSize = RequestedBatchSize;

            knowledge = sync.SyncKnowledge.Clone();
        }

        public override SyncIdFormatGroup IdFormats
        {
            get { return sync.IdFormats; }
        }
        /// <summary>
        /// Download Mechanism 
        /// </summary>
        /// <param name="resolutionPolicy"></param>
        /// <param name="sourceChanges"></param>
        /// <param name="changeDataRetriever"></param>
        /// <param name="syncCallback"></param>
        /// <param name="sessionStatistics"></param>
        public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, 
            object changeDataRetriever, SyncCallbacks syncCallback, SyncSessionStatistics sessionStatistics)
        {
            NetLog.Log.Info("Enter LocalStore::ProcessChangeBatch");
            
            ChangeBatch localVersions = sync.GetChanges(sourceChanges);

            ForgottenKnowledge destinationForgottenKnowledge = new ForgottenKnowledge(sync.IdFormats, sync.SyncKnowledge);

            NotifyingChangeApplier changeApplier = new NotifyingChangeApplier(sync.IdFormats);


            changeApplier.ApplyChanges(resolutionPolicy, CollisionConflictResolutionPolicy.Merge, sourceChanges,
        (IChangeDataRetriever)changeDataRetriever, localVersions, sync.SyncKnowledge.Clone(),
        destinationForgottenKnowledge, this, _memConflictLog, currentSessionContext, syncCallback);
            NetLog.Log.Info("End LocalStore::ProcessChangeBatch");
        }

        public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy,
            FullEnumerationChangeBatch sourceChanges,
            object changeDataRetriever, SyncCallbacks syncCallback, SyncSessionStatistics sessionStatistics)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public ulong GetNextTickCount()
        {
            return sync.GetNextTickCount();
        }

        #region IChangeDataRetriever Members

        public object LoadChangeData(LoadChangeContext loadChangeContext)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region INotifyingChangeApplierTarget Members

        public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
        {
    
          return sync.TryGetDestinationVersion(sourceChange, out destinationVersion);
        }
 
        public IChangeDataRetriever GetDataRetriever()
        {
            throw new NotImplementedException();
        }

        public void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgottenKnowledge)
        {
  
            sync.SyncKnowledge = knowledge;
            sync.ForgottenKnowledge = forgottenKnowledge;
            System.Diagnostics.Debug.WriteLine("Local.StoreKnowledgeForScope:" + knowledge + "ForgottenKnowledge:" + forgottenKnowledge);
        }

        #endregion

        public void SaveConstraintConflict(ItemChange conflictingChange, SyncId conflictingItemId, ConstraintConflictReason reason, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge, bool temporary)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Download Mechanism
        /// </summary>
        /// <param name="saveChangeAction"></param>
        /// <param name="change"></param>
        /// <param name="context"></param>
        public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
        {
            DataTransfer data = context.ChangeData as DataTransfer;
            if (data != null)
            {
                NetLog.Log.Info($"Enter LocalStore::SaveItemChange, trying to save Item {data.Uri} stream IsNull: {data.IsNull}");
                if (!data.IsNull)
                {
                
            
                    ItemMetadata item = sync.GetItemMetaData(saveChangeAction, change, data);
                    switch (saveChangeAction)
                    {
                        case SaveChangeAction.Create:
                        {
                            System.Diagnostics.Debug.WriteLine("Create File: " + item.Uri);
                            UpdateOrCreateFile(data, item);

                            break;
                        }
                        case SaveChangeAction.UpdateVersionAndData:
                        {
                            System.Diagnostics.Debug.WriteLine("UpdateVersion And Data File: " + item.Uri);
                            UpdateOrCreateFile(data, item);

                            break;
                        }
                        case SaveChangeAction.DeleteAndStoreTombstone:
                        {
                            System.Diagnostics.Debug.WriteLine("   Delete File: " + item.Uri);
                            string path = Path.Combine(folderPath, item.Uri);
                            if (item.Uri != "" && File.Exists(path))
                            {
                                File.Delete(path);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("   Delete File: " + item.Uri + " failled");

                            }
                            break;
                        }
                        default:
                        {
                            throw new NotImplementedException(saveChangeAction + " ChangeAction is not implemented!");
                        }

                    }
                    sync.GetUpdatedKnowledge(context);
                }
            }
        }

        private void UpdateOrCreateFile(DataTransfer data, ItemMetadata item)
        {
            NetLog.Log.Info("Enter LocalStore::UpdateOrCreateFile");

            try
            {
                string workingPath = GetPathToWorkWith();

                FileInfo fileInfo = new FileInfo(Path.Combine(workingPath, item.Uri));

                if (!fileInfo.Directory.Exists)
                    fileInfo.Directory.Create();


                using (
                    FileStream outputStream = new FileStream(Path.Combine(workingPath, item.Uri), FileMode.OpenOrCreate)
                    )
                {
                    int copyBlockSize = Convert.ToInt32(ConfigurationManager.AppSettings["copyBlockSize"]);
                    byte[] buffer = new byte[copyBlockSize];

                    int bytesRead;
                    while ((bytesRead = data.DataStream.Read(buffer, 0, copyBlockSize)) > 0)
                    {
                      //  NetLog.Log.Info("write Downloaded file On disk, bytesRead"+ bytesRead);
                        outputStream.Write(buffer, 0, bytesRead);
                    }
                    NetLog.Log.Info("write Downloaded file On disk, Ended" + bytesRead);
                    outputStream.SetLength(outputStream.Position);
                }
                item.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                GrantAccess(fileInfo.FullName);
                data.DataStream.Close();
                if (workingPath != folderPath /* useTemp*/) //useTemp Setting is true
                    fileInfo.MoveTo(Path.Combine(folderPath, fileInfo.Name));
            }
            catch (Exception ex)
            {

                Debug.Assert(false,"Hala 3ammi " + ex.Message);
            }
        }

        private string GetPathToWorkWith()
        {
            var useTemp = AppSettings.Get<bool>("UseTemp");
            string workingPath;
            if (useTemp)
                workingPath = AppSettings.Get<string>("TempDirectoryPath");
            else
                workingPath = folderPath;
            return workingPath;
        }

        public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
        {
            throw new NotImplementedException();
        }

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
    }
}
