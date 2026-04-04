namespace Cutulu.Core;

/// <summary>
/// A pathfinding map with 32x32 chunks with 32bit chunk side hashes. 
/// Heavily inspired by Tynan Sylvester's RimWorld.
/// Written, with blood, sweat and tears, by Maximilian Schecklmann April 2nd to 4th 2026. 
/// </summary>
public enum NAV_NEIGHBOUR : byte
{
    NORTH = 0,
    EAST = 1,
    SOUTH = 2,
    WEST = 3
}