﻿using System;
using System.IO;
using Microsoft.Synchronization;

namespace Common
{
     [Serializable()]
    public class LocalSyncDetails : Common.SyncDetails, IChangeDataRetriever
    {
         private string folderPath;
        public LocalSyncDetails(string folderPath, bool load)
            : base(folderPath, load) 
        {
            this.folderPath = folderPath;
        }

        public override object LoadChangeData(Microsoft.Synchronization.LoadChangeContext loadChangeContext)
        {
            NetLog.Log.Info("Start LocalSyncDetails::LoadChangeData");


            ItemMetadata item;
            metadataStore.TryGetItem(loadChangeContext.ItemChange.ItemId, out item);
            if (item.IsTombstone)
            {
                throw new InvalidOperationException("Cannot load change data for a deleted item.");
            }
            Stream dataStream=null;
            try
            {
                dataStream = new FileStream(Path.Combine(folderPath, item.Uri), FileMode.Open, FileAccess.Read, FileShare.Read);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            DataTransfer transferMechanism = new DataTransfer(dataStream, item.Uri);

            NetLog.Log.Info("End LocalSyncDetails::LoadChangeData");
            return transferMechanism;
        }
    }
}
