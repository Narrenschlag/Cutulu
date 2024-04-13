using System;
using System.Reflection;
using Archimedes;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Serves as middle man for Godot4 buttons and C# delegates.
    /// </summary>
    public partial class CallbackHolder : Node
    {
        public Action Callback;
        public Variant[] Args;

        public string FuncName;
        public Node Node;

        public CallbackHolder(Button button, Action callback, params Variant[] args) : this(callback, args)
        {
            button.ConnectButton(this, "Call");
            button.AddChild(this);
        }

        public CallbackHolder(string nodeFunc, Action callback, params Variant[] args) : this(callback, args)
        {
            this.Connect(nodeFunc, this, "Call");
        }

        public CallbackHolder(Button button, Node target, string funcName, params Variant[] args) : this(null, args)
        {
            button.AddChild(this);
            FuncName = funcName;
            Node = target;

            button.ConnectButton(this, "Call");
        }

        public CallbackHolder(Action callback, params Variant[] args)
        {
            Callback = callback;
            Args = args;
        }

        public void Call()
        {
            if (Node.NotNull())
            {
                Node.Call(FuncName, Args);
            }

            else
            {
                if (Args.NotEmpty()) Callback?.Invoke(Callback.GetMethodInfo().Name, Args);
                else Callback?.Invoke();
            }
        }
    }
}