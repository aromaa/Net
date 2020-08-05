using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Net.Sockets.Async
{
    internal abstract class SocketAwaitableEventArgs<T> : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action CompletedCallback = () => { };
        private static readonly Action<object> RunContinuationCallbackAction = SocketAwaitableEventArgs<T>.RunContinuationCallback;

        private PipeScheduler Scheduler { get; }

        private Action? Callback;

        protected SocketAwaitableEventArgs(PipeScheduler scheduler) : base(unsafeSuppressExecutionContextFlow: true)
        {
            this.Scheduler = scheduler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ResetCallback()
        {
            this.Callback = null;
        }

        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            Action? continuation = Interlocked.Exchange(ref this.Callback, SocketAwaitableEventArgs<T>.CompletedCallback);
            if (continuation != null)
            {
                this.RunContinuation(continuation);
            }
        }

        public void OnCompleted(Action continuation)
        {
            if (object.ReferenceEquals(Volatile.Read(ref this.Callback), SocketAwaitableEventArgs<T>.CompletedCallback) || object.ReferenceEquals(Interlocked.CompareExchange(ref this.Callback, continuation, null), SocketAwaitableEventArgs<T>.CompletedCallback))
            {
                this.RunContinuation(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation) => this.OnCompleted(continuation);

        private void RunContinuation(Action continuation)
        {
            this.Scheduler.Schedule(SocketAwaitableEventArgs<T>.RunContinuationCallbackAction, continuation);
        }

        private static void RunContinuationCallback(object state) => ((Action)state).Invoke();

        public virtual SocketAwaitableEventArgs<T> GetAwaiter() => this;

        public bool IsCompleted => object.ReferenceEquals(this.Callback, SocketAwaitableEventArgs<T>.CompletedCallback);
        public abstract T GetResult();
    }
}
