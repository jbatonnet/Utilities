using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

public class ProfileScope : IDisposable
{
#if DEBUG
    public DateTime start;
    public string label;
#endif

    public ProfileScope([CallerMemberName]string label = null)
    {
#if DEBUG
        start = DateTime.UtcNow;
        this.label = label;
#endif
    }
    public void Dispose()
    {
#if DEBUG
        TimeSpan duration = DateTime.UtcNow - start;
        Log.Trace((label ?? "") + " " + duration.TotalMilliseconds);
#endif
    }
}