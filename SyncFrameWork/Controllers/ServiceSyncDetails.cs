using System;
using System.Diagnostics;
using System.IO;
using Common;
using Microsoft.Synchronization;
using SyncFrameWork.Controllers;
using SyncFrameWork.SyncService;

namespace SyncFrameWork
{
    public class RemoteSyncDetails : Common.SyncDetails, IChangeDataRetriever
    {
        public RemoteSyncDetails(string folderPath, bool load)
            : base(folderPath, load)
        {
        }
        private SyncServiceClient service;
        
        public SyncServiceClient Service
        {
            get
            {  
                return service;
            }
            set
            {
                if (service == null)
                {
                    service = new SyncServiceClient();
                }
                service = value;
            }
        }

        public override object LoadChangeData(Microsoft.Synchronization.LoadChangeContext loadChangeContext)
        {
            NetLog.Log.Info("Start RemoteSyncDetails::LoadChangeData");

            ItemMetadata item;

            metadataStore.TryGetItem(loadChangeContext.ItemChange.ItemId, out item);

            if (item.IsTombstone)
            {
                throw new InvalidOperationException("Cannot load change data for a deleted item.");
            }

            System.Diagnostics.Debug.WriteLine("Downloading File:" + item.Uri);
            NetLog.Log.Info("Downloading File:" + item.Uri);
            NetLog.FireMessage(this, new MessageEventArgs(item.Uri, Operation.Download, Status.Started));
            Stream downloadFile =null;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                downloadFile = service.DownloadFile(item.Uri);
                watch.Stop();
                Debug.WriteLine($"downloadFile ElapsedMilliseconds {watch.ElapsedMilliseconds}");
            }
            catch (Exception e)
            {
                NetLog.ErrorHappened(this, new ErrorEventArgs(e));
                Console.WriteLine(e);
            }
            NetLog.FireMessage(this, new MessageEventArgs(item.Uri, Operation.Download, Status.Finished));
            NetLog.Log.Info("Downloading File:" + item.Uri + " Done Succsessfully, Next Step Write it on disk");
            var transferMechanism = new DataTransfer(downloadFile, item.Uri);
            NetLog.Log.Info("End RemoteSyncDetails::LoadChangeData");
            return transferMechanism;
        }

    }
    
}
