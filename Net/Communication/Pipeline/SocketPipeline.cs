using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.NetworkInformation;
using System.Text;
using Net.Communication.Incoming.Handlers;
using Net.Communication.Outgoing.Handlers;
using Net.Communication.Outgoing.Helpers;

namespace Net.Communication.Pipeline
{
    public class SocketPipeline
    {
        private IOrderedDictionary Handlers;

        public SocketPipeline() : this(new OrderedDictionary())
        {
        }

        private SocketPipeline(IOrderedDictionary handlers)
        {
            this.Handlers = handlers;
        }

        public void AddHandlerLast(IPipelineHandler handler)
        {
            this.Handlers.Add(handler, handler);
        }

        public void AddHandlerFirst(IPipelineHandler handler)
        {
            this.Handlers.Insert(0, handler, handler);
        }

        public void RemoveHandler(IPipelineHandler handler)
        {
            this.Handlers.Remove(handler);
        }

        public bool? HandleIn<T>(ref SocketPipelineContext context, ref T data, int index)
        {
            if (this.Handlers.Count > index)
            {
                IPipelineHandler handler = (IPipelineHandler)this.Handlers[index];
                if (handler is IIncomingObjectHandler objectHandler)
                {
                    objectHandler.Handle(ref context, ref data);

                    return false;
                }

                return null;
            }
            else
            {
                return true;
            }
        }

        public bool? HandleOut<T>(ref SocketPipelineContext context, in T data, int index, ref PacketWriter writer)
        {
            if (this.Handlers.Count > index)
            {
                if (this.Handlers[index] is IOutgoingObjectHandler objectHandler)
                {
                    objectHandler.Handle(ref context, in data, ref writer);

                    return false;
                }

                return null;
            }
            else
            {
                return true;
            }
        }
    }
}
