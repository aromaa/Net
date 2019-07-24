using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Connections
{
    public class SocketAwaitableEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action CompletedCallback = () => { };
        private static readonly Action<object> RunContinuationCallbackAction = SocketAwaitableEventArgs.RunContinuationCallback;

        private static readonly FieldInfo FlowExecutionContextFieldInfo = typeof(SocketAsyncEventArgs).GetField("_flowExecutionContext", BindingFlags.NonPublic | BindingFlags.Instance);

        private PipeScheduler Scheduler { get; }

        private Action? Callback;

        public SocketAwaitableEventArgs(PipeScheduler scheduler)
        {
            this.Scheduler = scheduler;

            SocketAwaitableEventArgs.FlowExecutionContextFieldInfo.SetValue(this, false);
        }

        public bool IsCompleted => object.ReferenceEquals(this.Callback, SocketAwaitableEventArgs.CompletedCallback);

        public SocketAwaitableEventArgs GetAwaiter() => this;

        public int GetResult()
        {
            this.Callback = null;

            return this.BytesTransferred;
        }

        public void OnCompleted(Action continuation)
        {
            if (object.ReferenceEquals(Volatile.Read(ref this.Callback), SocketAwaitableEventArgs.CompletedCallback) || object.ReferenceEquals(Interlocked.CompareExchange(ref this.Callback, continuation, null), SocketAwaitableEventArgs.CompletedCallback))
            {
                this.RunContinuation(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation) => this.OnCompleted(continuation);

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            Action? continuation = Interlocked.Exchange(ref this.Callback, SocketAwaitableEventArgs.CompletedCallback);
            if (continuation != null)
            {
                this.RunContinuation(continuation);
            }
        }

        private void RunContinuation(Action continuation)
        {
            this.Scheduler.Schedule(SocketAwaitableEventArgs.RunContinuationCallbackAction, continuation);
        }

        private static void RunContinuationCallback(object state)
        {
            ((Action)state).Invoke();
        }
    }
}
