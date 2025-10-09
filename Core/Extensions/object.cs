namespace Cutulu.Core;

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

#if GODOT4_0_OR_GREATER
using Godot;
#endif

public static class objectExtension
{
    public static bool NotNull(this object obj) => !IsNull(obj);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull(this object obj)
    {
#if GODOT4_0_OR_GREATER
        return obj == null
        || (obj is Disposable d && d.IsDisposed())
        || (obj is GodotObject gd && (!GodotObject.IsInstanceValid(gd) || gd.IsQueuedForDeletion()));
#else
        return obj == null
        || (obj is Disposable d && d.IsDisposed())
#endif
    }

    public static void Destroy(this object obj, bool forceInstant = false)
    {
        // Handle interface
        if (obj is IDestoryable i) i.Destroyed();

        switch (obj)
        {
#if GODOT4_0_OR_GREATER
            case Node n when n.NotNull():
                if (forceInstant) n.Free();
                else n.QueueFree();
                break;

            case GodotObject gd when gd.NotNull():
                gd.Dispose();
                break;
#endif

            case IDisposable d:
                d.Dispose();
                break;
        }
    }

    public static async void Destroy(this object obj, float lifeTime, bool forceInstant = false)
    {
        await Task.Delay(Mathf.RoundToInt(lifeTime * 1000));

        if (obj == null) return;

        lock (obj) Destroy(obj, forceInstant);
    }

    public static void Destroy<T>(this IEnumerable<T> objs, bool forceInstant = false)
    {
        if (objs == null) return;

        foreach (var obj in objs)
            Destroy(obj, forceInstant);
    }
}