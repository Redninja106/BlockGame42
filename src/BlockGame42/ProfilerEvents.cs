using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;

[EventSource(Name = "BlockGame42-Profiler")]
public sealed class ProfilerEvents : EventSource
{
    public static ProfilerEvents Instance = new();

    [Event(1), Conditional("PROFILING")]
    public void BeginSection(string name) => WriteEvent(1, name);

    [Event(2), Conditional("PROFILING")]
    public void EndSection(string name) => WriteEvent(2, name);
}
