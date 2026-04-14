namespace Cutulu.Web;

using System.Diagnostics;
using System.Threading;
using System;
using Core;

public class Processing
{
    private readonly SwapbackArray<IProcessable> _updateMethods = [];
    private bool _running;

    public void AddUpdate(IProcessable processable)
    {
        _updateMethods.Add(processable);
    }

    public void Run()
    {
        if (_running) return;
        _running = true;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        SwapbackArray<int> toRemove = [];
        IProcessable? update;

        float tickFrequency = (float)Stopwatch.Frequency, deltaTime;
        long lastTime = stopwatch.ElapsedTicks, now;
        int i;

        while (_running)
        {
            now = stopwatch.ElapsedTicks;
            deltaTime = (now - lastTime) / tickFrequency;
            lastTime = now;

            // Call updates
            for (i = 0; i < _updateMethods.Count; i++)
            {
                update = _updateMethods[i];

                if (update == null) toRemove.Add(i);
                else update.Process(deltaTime);
            }

            // Removes null instances
            for (i = toRemove.Count - 1; i >= 0; i--)
            {
                _updateMethods.RemoveAt(toRemove[i]);
            }

            Thread.Sleep(1); // prevents 100% CPU usage
        }
    }

    public void Stop()
    {
        _running = false;
    }
}

public interface IProcessable
{
    public void Process(float deltaTime);
}