#if GODOT4_0_OR_GREATER

namespace Cutulu.Prototyping;

using Godot;

public partial class TextEdit : Godot.TextEdit
{
    public readonly Core.Notification<string> TextChanged2 = new();

    [Export] private bool ForceAscii = true;
    [Export] public int CharLimit = 128;
    [Export] public int LineLimit = 3;

    private bool SetupComplete = false;

    public override void _Ready()
    {
        Setup();
    }

    private void Setup()
    {
        if (SetupComplete) return;
        SetupComplete = true;

        TextChanged += OnTextChanged;
    }

    private void OnTextChanged()
    {
        string old_text = Text;

        string[] lines = old_text.Split('\n');

        if (ForceAscii || old_text.Length > CharLimit || lines.Length > LineLimit)
        {
            old_text = default;
            bool hit = false;
            int caret = 0;

            for (int i = 0; i < lines.Length && i < LineLimit; i++)
            {
                if (i > 0)
                {
                    old_text += "\n";
                    caret++;
                }

                if (ForceAscii)
                    foreach (char c in lines[i])
                    {
                        if (char.IsAscii(c) || c == ' ' || c == '\n')
                        {
                            old_text += c;
                            caret++;
                        }
                        else hit = true;
                    }

                else old_text += lines[i];
            }

            if (hit || old_text.Length > CharLimit || lines.Length > LineLimit)
            {
                old_text = old_text.Trim();
                Text = old_text = old_text[..Mathf.Min(CharLimit, old_text.Length)];

                SetCaretLine(lines.Length - 1);
                SetCaretColumn(caret);
            }
        }

        TextChanged2.Invoke(old_text);
    }
}

#endif