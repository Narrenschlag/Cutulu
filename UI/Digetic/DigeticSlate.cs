namespace Cutulu.UI;

using Godot;
using Core;

public partial class DigeticSlate : DigeticInterface
{
    [Export] private SubViewport UIContainer;

    [Export] private CollisionShape3D SlateShape;
    [Export] private Area3D MouseArea;

    [Export] private MeshInstance3D RenderTarget;
    [Export] private int RenderTargetMaterialIdx = 0;

    public override bool TryGetMappingSurface(Vector3 globalPos, Area3D area, out CollisionShape3D shape, out SubViewport viewport)
    {
        viewport = UIContainer;
        shape = SlateShape;
        return true;
    }

    public override void _Ready()
    {
        if (RenderTarget.IsNull()) return;

        RegisterArea(MouseArea);

        ApplyMaterial(RenderTarget, RenderTargetMaterialIdx, UIContainer);
    }
}