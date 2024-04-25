using Godot;

namespace Cutulu
{
    [GlobalClass]
    public partial class MonoButton : Button
    {
        [Export] public Node Target { get; set; }
        [Export] public string FuncName { get; set; }

        public override void _EnterTree()
        {
            base._EnterTree();

            if (Target.IsNull()) return;
            this.ConnectButton(Target, FuncName);
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            if (Target.IsNull()) return;
            this.DisconnectButton(Target, FuncName);
        }
    }
}