namespace System.Threading.Tasks
{
    public class None { }
    public class TaskCompletionSource : TaskCompletionSource<None>
    {
        public void SetCompleted()
        {
            SetResult(null);
        }
        public void TrySetCompleted()
        {
            TrySetResult(null);
        }
    }

    public static class TaskExtensions
    {
        public static Task CancelAfter(this Task me, int millisecondsTimeout, CancellationTokenSource cancellationTokenSource = null)
        {
            return CancelAfter(me, TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationTokenSource);
        }
        public static Task CancelAfter(this Task me, TimeSpan timeout, CancellationTokenSource cancellationTokenSource = null)
        {
            if (me.IsCompleted || (timeout == TimeSpan.MaxValue))
                return me;

            TaskCompletionSource taskCompletionSource = new TaskCompletionSource();

            if (timeout.Ticks == 0)
            {
                taskCompletionSource.SetCanceled();
                return taskCompletionSource.Task;
            }

            CancellationTokenSource delayCancellationTokenSource = new CancellationTokenSource();

            Task.WhenAny(me, Task.Delay(timeout, delayCancellationTokenSource.Token))
                .ContinueWith(t =>
                {
                    if (me.IsCompleted)
                    {
                        delayCancellationTokenSource.Cancel();
                        taskCompletionSource.SetCompleted();
                    }
                    else
                    {
                        if (cancellationTokenSource != null)
                            cancellationTokenSource.Cancel();

                        taskCompletionSource.SetCanceled();
                    }
                });

            return taskCompletionSource.Task;
        }
        public static Task<TResult> CancelAfter<TResult>(this Task<TResult> me, int millisecondsTimeout, CancellationTokenSource cancellationTokenSource = null)
        {
            return CancelAfter(me, TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationTokenSource);
        }
        public static Task<TResult> CancelAfter<TResult>(this Task<TResult> me, TimeSpan timeout, CancellationTokenSource cancellationTokenSource = null)
        {
            if (me.IsCompleted || (timeout == TimeSpan.MaxValue))
                return me;

            TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();

            if (timeout.Ticks == 0)
            {
                taskCompletionSource.SetCanceled();
                return taskCompletionSource.Task;
            }

            CancellationTokenSource delayCancellationTokenSource = new CancellationTokenSource();

            Task.WhenAny(me, Task.Delay(timeout, delayCancellationTokenSource.Token))
                .ContinueWith(t =>
                {
                    if (me.IsCompleted)
                    {
                        delayCancellationTokenSource.Cancel();
                        taskCompletionSource.SetResult(me.Result);
                    }
                    else
                    {
                        if (cancellationTokenSource != null)
                            cancellationTokenSource.Cancel();

                        taskCompletionSource.SetCanceled();
                    }
                });

            return taskCompletionSource.Task;
        }
    }
}