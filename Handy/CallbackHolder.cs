using System;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Serves as middle man for Godot4 buttons and C# delegates.
    /// </summary>
    public partial class CallbackHolder : Node
    {
        public Action Callback;

        public CallbackHolder(Button button, Action callback) : this(callback)
        {
            button.ConnectButton(this, "Call");
            button.AddChild(this);
        }

        public CallbackHolder(Action callback)
        {
            Callback = callback;
        }

        public void Call()
        {
            Callback?.Invoke();
        }
    }
}