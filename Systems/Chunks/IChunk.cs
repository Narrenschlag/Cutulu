namespace Cutulu.Systems.Chunks;

public interface IChunk<M, C>
    where M : ChunkManager<M, C>
    where C : IChunk<M, C>
{
    C Init(M manager, ChunkPoint point);
}