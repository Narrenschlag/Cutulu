namespace Cutulu.Core;

public interface IDestoryable
{
    /// <summary>
    /// Notification right before object.Destroy() takes effect.
    /// </summary>
    public void Destroyed();
}