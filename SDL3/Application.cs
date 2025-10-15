using bottlenoselabs.Interop;
using Interop.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace SDL;

public abstract class Application
{
    private bool windowClosed = false;

    protected abstract void OnInit();
    protected abstract void OnFrame(float deltaTime);

    public static ulong GetTicks()
    {
        return SDL_GetTicks();
    }
    public static ulong GetTicksNS()
    {
        return SDL_GetTicksNS();
    }

    public void Run()
    {
        SDL_Init(SDL_INIT_VIDEO).ThrowIfFailed();

        unsafe
        {
            SDL_SetLogPriority((int)SDL_LogCategory.SDL_LOG_CATEGORY_GPU, SDL_LogPriority.SDL_LOG_PRIORITY_WARN);
            SDL_SetLogOutputFunction(new(&LogOutput), null);
        }

        SDL_LogWarn(0, "testing again"u8);

        OnInit();
        ulong lastTime = GetPerformanceCounter();
        while (!windowClosed)
        {
            ulong currentTime = GetPerformanceCounter();
            ulong deltaTime = currentTime - lastTime;
            lastTime = currentTime;

            SDL_PumpEvents();
            ProcessEvents();

            OnFrame(deltaTime / (float)GetPerformanceFrequency());
        }

        SDL_Quit();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void LogOutput(void* userdata, int category, SDL_LogPriority priority, CString message)
    {
        double time = GetTicks() / 1000.0;
        string priorityName = priority.ToString()[17..];
        string categoryName = ((SDL_LogCategory)category).ToString()[17..];
        Console.WriteLine($"[{time:f3}][{priorityName}][{categoryName}]: {CString.ToString(message)}");
    }

    public ulong GetPerformanceCounter()
    {
        return SDL_GetPerformanceCounter();
    }

    public ulong GetPerformanceFrequency()
    {
        return SDL_GetPerformanceFrequency();
    }

    unsafe void ProcessEvents()
    {
        SDL_Event ev = default;
        while (SDL_PollEvent(&ev))
        {
            OnEvent(ev);
        }
    }

    private void OnEvent(in SDL_Event ev)
    {
        switch ((SDL_EventType)ev.type)
        {
            case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                windowClosed = true;
                break;
            default:
                break;
        }
    }
}
