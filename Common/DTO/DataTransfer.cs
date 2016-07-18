
using System.IO;

namespace Common
{
    public class DataTransfer
    {
        private System.IO.Stream dataStream;
        private bool isNull;
        private string uri;

        public DataTransfer(System.IO.Stream dataStream)
        {
            this.dataStream = dataStream;
        }

        public DataTransfer(System.IO.Stream dataStream, string uri)
        {
            this.dataStream = dataStream;
            this.uri = uri;
            isNull = dataStream == null;
        }

        public DataTransfer(Stream dataStream, string uri, bool isNull) : this(dataStream, uri)
        {
            this.isNull = isNull;
        }

        public System.IO.Stream DataStream
        {
            get { return dataStream; }
        }

        public string Uri
        {
            get { return uri; }
        }

        public bool IsNull
        {
            get { return isNull; }
        }
    }
}
