using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockGame42;

/// <summary>
/// Wraps a value from Application.GetTicksNS() to simplify time calculations.
/// </summary>
internal struct Timer
{
    private ulong startTicksNS;

    public static Timer Start()
    {
        return new Timer { startTicksNS = Application.GetTicksNS() };
    }

    public double ElapsedSeconds()
    {
        return ElapsedNanoseconds() / 1_000_000_000.0;
    }

    public double ElapsedMilliseconds()
    {
        return ElapsedNanoseconds() / 1_000_000.0;
    }

    public double ElapsedMicroseconds()
    {
        return ElapsedNanoseconds() / 1000.0;
    }

    public ulong ElapsedNanoseconds()
    {
        return Application.GetTicksNS() - startTicksNS;
    }
}
