using System;
using System.IO;
using log4net;

namespace Common
{
    public sealed class NetLog
    {
        public static event ErrorHappenedtHandler OnErrorHappened;
        public static event MessageEventHandler OnMessageFired;
        private static readonly log4net.ILog myLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private NetLog() {
            try
            {
                log4net.Config.XmlConfigurator.Configure();
                //Log.Info("Hello World"); 
            }
            catch (Exception ex)
            {
                Log.Error("OnStartup", ex);
            }
        }

        public static ILog Log
        {
            get { return myLog; }
        }

        public static void FireMessage(Object obj,MessageEventArgs eventArgs)
        {
            if (OnMessageFired != null)
            {
                OnMessageFired(obj, eventArgs);
            }
        }

        public static void ErrorHappened(Object obj, ErrorEventArgs eventArgs)
        {
            if (OnErrorHappened != null)
            {
                OnErrorHappened(obj, eventArgs);
            }
        }
    }

    public delegate void MessageEventHandler(object sender, MessageEventArgs args);
    public delegate void ErrorHappenedtHandler(object sender, ErrorEventArgs args);


    //This is a class which describes the event to the class that recieves it.
    //An EventArgs class must always derive from System.EventArgs.
    public class MessageEventArgs : EventArgs
    {
        public string ItemUri { get; private set; }
        public Operation Operation { get; private set; }
        public Status Status { get; private set; }
        public object Data { get; private set; }


        public MessageEventArgs(string itemUri, Operation operation, Status status, object data)
        {
            ItemUri = itemUri;
            Operation = operation;
            Status = status;
            Data = data;
        }

        public MessageEventArgs()
        {
        }

        public MessageEventArgs(string itemUri, Operation operation, Status status)
        {
            ItemUri = itemUri;
            Operation = operation;
            Status = status;
        }
    }

    public enum Operation
    {
        Upload,Download
    }

    public enum Status
    {
        Started,Finished
    }
}