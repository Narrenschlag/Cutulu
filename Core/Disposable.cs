namespace Cutulu.Core;

using System;

public interface Disposable : IDisposable
{
    public bool IsDisposed();
}