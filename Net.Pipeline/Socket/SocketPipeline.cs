using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Net.Pipeline.Handler;

namespace Net.Pipeline.Socket
{
    public class SocketPipeline
    {
        //TODO: Do something about thread safety
        internal LinkedList<IPipelineHandler?> Handlers;

        public SocketPipeline()
        {
            this.Handlers = new LinkedList<IPipelineHandler?>();
            this.Handlers.AddLast((IPipelineHandler?)null); //Hacky workaround to allow adding handlers while in use
        }

        public void AddHandlerFirst(IPipelineHandler handler)
        {
            this.Handlers.AddFirst(handler);
        }

        public void AddHandlerLast(IPipelineHandler handler)
        {
            this.Handlers.Last!.Value = handler;
            this.Handlers.AddLast((IPipelineHandler?)null);
        }

        public void RemoveHandler(IPipelineHandler handler)
        {
            this.Handlers.Remove(handler);
        }

        public Enumerator GetEnumerator() => new Enumerator(this.Handlers);

        public struct Enumerator : IEnumerator<IPipelineHandler>
        {
            private LinkedListNode<IPipelineHandler?> Node;
            [AllowNull] private IPipelineHandler Value;

            private int Index;

            public Enumerator(LinkedList<IPipelineHandler?> list)
            {
                this.Node = list.First!;
                this.Value = default;

                this.Index = 0;
            }

            public IPipelineHandler Current => this.Value;

            object IEnumerator.Current
            {
                get
                {
                    if (this.Index == 0 || this.Index == this.Node.List!.Count)
                    {
                        throw new InvalidOperationException();
                    }

                    return this.Value;
                }
            }

            public bool MoveNext()
            {
                if (this.Node.Value == null)
                {
                    this.Index = this.Node.List!.Count;

                    return false;
                }

                this.Index++;

                this.Value = this.Node.Value;
                this.Node = this.Node.Next!;

                return true;
            }

            public void Dispose()
            {

            }

            public void Reset()
            {
                this.Node = this.Node.List!.First!;
                this.Value = default!;

                this.Index = 0;
            }
        }
    }
}
