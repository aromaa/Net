using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Net.Listeners
{
    public class ListenerConfig
    {
        private static readonly IPAddress DefaultAddress = IPAddress.Any;
        private const ushort DefaultPort = 0;

        private const int DefaultBacklog = 100;

        public bool IsReadOnly { get; private set; }
        
        private IPEndPoint? IPEndPoint_;
        public IPEndPoint IPEndPoint
        {
            get => this.IPEndPoint_ ?? (this.IPEndPoint_ = new IPEndPoint(ListenerConfig.DefaultAddress, ListenerConfig.DefaultPort));
            set
            {
                this.CheckForReadOnly();

                this.IPEndPoint_ = value;
            }
        }

        [DataMember(Name = "address")]
        public IPAddress Address
        {
            get => this.IPEndPoint.Address;
            set
            {
                this.CheckForReadOnly();

                if (this.IPEndPoint_ == null)
                {
                    this.IPEndPoint_ = new IPEndPoint(value, ListenerConfig.DefaultPort);
                }
                else
                {
                    this.IPEndPoint_.Address = value;
                }
            }
        }

        [DataMember(Name = "port")]
        public ushort Port
        {
            get => (ushort)this.IPEndPoint.Port;
            set
            {
                this.CheckForReadOnly();

                if (this.IPEndPoint_ == null)
                {
                    this.IPEndPoint_ = new IPEndPoint(ListenerConfig.DefaultAddress, value);
                }
                else
                {
                    this.IPEndPoint_.Port = value;
                }
            }
        }

        [DataMember(Name = "backlog")]
        private int Backlog_ = ListenerConfig.DefaultBacklog;
        public int Backlog
        {
            get => this.Backlog_;
            set
            {
                this.CheckForReadOnly();

                this.Backlog_ = value;
            }
        }

        public void MakeReadOnly()
        {
            this.IsReadOnly = true;
        }

        private void CheckForReadOnly()
        {
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("Read-only");
            }
        }

        public ListenerConfig Clone()
        {
            return (ListenerConfig)this.MemberwiseClone();
        }
    }
}
