namespace Cutulu.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Godot;

    using Cutulu.Network;

    public partial class IntegrationTest : Node
    {
        [Export] public bool IsSingleTest { get; set; } = true;

        protected virtual int StepCount => 1;
        protected int Step = 0;

        public override void _Ready()
        {
            if (IsSingleTest)
                _ = Start();
        }

        public virtual async Task<bool> Start()
        {
            return await _Process();
        }

        protected virtual void NextStep()
        {
            Print(Colors.PaleGreen, $"Step {++Step}/{StepCount} succeeded.");
        }

        protected virtual async Task EndTest(bool success, string message = default)
        {
            Step++;

            Print(success ? Colors.LimeGreen : Colors.IndianRed, message.NotEmpty() ? message : success ?
            $"Test succeeded. {Step}/{StepCount} steps have been completed successfully." :
            $"Test failed. {Step}/{StepCount} had an issue.");

            if (IsSingleTest)
            {
                Application.Quit();
            }

            await Task.Delay(0);
        }

        public virtual void PrintErr(string message) => Print(Colors.PaleVioletRed, message);
        public virtual void Print(string message) => Print(Colors.DimGray, message);
        public virtual void Print(Color color, string message)
        {
            Debug.LogR($"[color={color.ToHtml()}][{Name}] {message}[/color]");
        }

        protected virtual async Task<bool> _Process()
        {
            await Task.Delay(0);
            return true;
        }
    }
}