namespace Cutulu.UX
{
    using Godot;

    [GlobalClass]
    public partial class OptionButton : RichTextLabel
    {
        private new string Text;
        private Color Color;

        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public virtual void Setup(GenericOptionMenu parent, string text, Color color)
        {
            parent.AddChild(this);
            BbcodeEnabled = true;

            AutowrapMode = TextServer.AutowrapMode.Off;
            ScrollActive = false;
            Size = Vector2.Zero;
            FitContent = true;

            SetText(text);
            SetColor(color);

            Position -= GetRect().Size * 0.5f;
        }

        public void SetColor(Color color)
        {
            Color = color;
            SetText(Text);
        }

        public void UpdateText(Color color, string prefix = "", string suffix = "")
        {
            Prefix = prefix;
            Suffix = suffix;
            SetColor(color);
        }

        public new void SetText(string text)
        {
            Name = $"Option {text}";
            Text = text;

            Position += GetRect().Size * 0.5f;
            base.Text = $"[center][color={Color.ToHtml()}]{Prefix}{text}{Suffix}";
            Position -= GetRect().Size * 0.5f;
        }
    }
}