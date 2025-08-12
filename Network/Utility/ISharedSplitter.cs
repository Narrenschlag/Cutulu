namespace Cutulu.Network;

using System;
using Godot;

public partial interface ISharedSplitter : IShared
{
    public void Split(Node parent, bool asClient);
}