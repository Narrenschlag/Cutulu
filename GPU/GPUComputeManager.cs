#if GODOT4_0_OR_GREATER
namespace Cutulu.GPU;

using System.Collections.Generic;
using Cutulu.Core;
using System.IO;
using Godot;

/// <summary>
/// Optimized GPU compute shader manager for voxel terrain processing
/// </summary>
public class GPUComputeManager : Disposable
{
    // Reusable resources - initialize once, use many times
    private RenderingDevice _local_rendering_device;
    private RenderingDevice GetRd() => _local_rendering_device ??= RenderingServer.GetRenderingDevice();

    private readonly Dictionary<string, ComputeShaderCache> shaderCache = [];

    // Pool of reusable buffers to avoid constant allocation
    private readonly Dictionary<int, Queue<Rid>> bufferPools = [];

    private bool Disposed = false;

    public bool IsDisposed() => Disposed;

    public void Dispose()
    {
        if (Disposed) return;
        Disposed = true;

        // Clean up all resources
        CleanupShaderCache();
        CleanupBufferPools();

        GetRd()?.Free();
    }

    /// <summary>
    /// Load and cache a compute shader for reuse
    /// </summary>
    public bool LoadShader(string name, string shaderPath)
    {
        if (shaderCache.ContainsKey(name))
            return true;

        var shaderFile = GD.Load<RDShaderFile>(shaderPath);
        if (shaderFile.IsNull())
        {
            Debug.LogError($"Failed to load shader: {shaderPath}");
            return false;
        }

        var shaderBytecode = shaderFile.GetSpirV();
        var shader = GetRd().ShaderCreateFromSpirV(shaderBytecode);
        var pipeline = GetRd().ComputePipelineCreate(shader);

        shaderCache[name] = new ComputeShaderCache
        {
            Shader = shader,
            Pipeline = pipeline
        };

        return true;
    }

    private RDUniform GetUniform(Rid rid, int binding, RenderingDevice.UniformType type = RenderingDevice.UniformType.StorageBuffer)
    {
        var uniform = new RDUniform
        {
            UniformType = type,
            Binding = binding,
        };

        uniform.AddId(rid);
        return uniform;
    }

    /// <summary>
    /// Execute a compute shader with input data
    /// Optimized for batch processing - reuses buffers when possible
    /// </summary>
    public T[] ExecuteCompute<T>(
        string shaderName,
        T[] inputData,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1,
        bool reuseBuffer = true) where T : unmanaged
    {
        if (shaderCache.TryGetValue(shaderName, out var shader) == false)
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        // Encode input data
        var buffer = Encode(inputData);

        // Get or create buffer
        if (reuseBuffer && TryGetPooledBuffer(buffer.Length, out Rid bufferRid))
            // Reuse existing buffer, just update data
            GetRd().BufferUpdate(bufferRid, 0, (uint)buffer.Length, buffer);
        else
            bufferRid = GetRd().StorageBufferCreate((uint)buffer.Length, buffer);

        // Create uniform set
        var uniform = GetUniform(bufferRid, 0, RenderingDevice.UniformType.StorageBuffer);

        var uniformSet = GetRd().UniformSetCreate([uniform], shader.Shader, 0);

        // Execute compute shader
        var computeList = GetRd().ComputeListBegin();
        GetRd().ComputeListBindComputePipeline(computeList, shader.Pipeline);
        GetRd().ComputeListBindUniformSet(computeList, uniformSet, 0);
        GetRd().ComputeListDispatch(computeList, (uint)workGroupsX, (uint)workGroupsY, (uint)workGroupsZ);
        GetRd().ComputeListEnd();

        // Submit and wait for completion
        GetRd().Submit();
        GetRd().Sync();

        // Read results
        var outputBuffer = GetRd().BufferGetData(bufferRid);
        var output = Decode<T>(outputBuffer);

        // Cleanup uniform set (always needed)
        GetRd().FreeRid(uniformSet);

        // Return buffer to pool or free it
        if (reuseBuffer)
        {
            ReturnBufferToPool(buffer.Length, bufferRid);
        }
        else
        {
            GetRd().FreeRid(bufferRid);
        }

        return output;
    }

    /// <summary>
    /// Execute compute shader with separate input and output buffers
    /// Useful for operations that need more output space than input
    /// </summary>
    public T[] ExecuteComputeSeparateBuffers<T>(
        string shaderName,
        T[] inputData,
        int outputSize,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1) where T : unmanaged
    {
        if (!shaderCache.TryGetValue(shaderName, out var cache))
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        // Create input buffer
        var inputBuffer = Encode(inputData);
        var inputRid = GetRd().StorageBufferCreate((uint)inputBuffer.Length, inputBuffer);

        // Create output buffer (empty, just reserve space)
        var outputBufferSize = outputSize * System.Runtime.InteropServices.Marshal.SizeOf<T>();
        var outputRid = GetRd().StorageBufferCreate((uint)outputBufferSize);

        // Create uniforms for both buffers
        var inputUniform = GetUniform(inputRid, 0, RenderingDevice.UniformType.StorageBuffer);
        var outputUniform = GetUniform(outputRid, 1, RenderingDevice.UniformType.StorageBuffer);

        var uniformSet = GetRd().UniformSetCreate([inputUniform, outputUniform], cache.Shader, 0);

        // Execute
        var computeList = GetRd().ComputeListBegin();
        GetRd().ComputeListBindComputePipeline(computeList, cache.Pipeline);
        GetRd().ComputeListBindUniformSet(computeList, uniformSet, 0);
        GetRd().ComputeListDispatch(computeList, (uint)workGroupsX, (uint)workGroupsY, (uint)workGroupsZ);
        GetRd().ComputeListEnd();

        GetRd().Submit();
        GetRd().Sync();

        // Read output
        var outputBuffer = GetRd().BufferGetData(outputRid);
        var output = Decode<T>(outputBuffer);

        // Cleanup
        GetRd().FreeRid(uniformSet);
        GetRd().FreeRid(inputRid);
        GetRd().FreeRid(outputRid);

        return output;
    }

    /// <summary>
    /// Execute compute shader with push constants and separate buffers
    /// OPTIMIZED for voxel generation - no input buffer, only push constants
    /// </summary>
    public T[] ExecuteComputeWithPushConstants<T, TPushConstants>(
        string shaderName,
        TPushConstants pushConstants,
        int outputSize,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1) where T : unmanaged where TPushConstants : unmanaged
    {
        if (!shaderCache.TryGetValue(shaderName, out var cache))
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        // Create output buffer only
        var outputBufferSize = outputSize * System.Runtime.InteropServices.Marshal.SizeOf<T>();
        var outputRid = GetRd().StorageBufferCreate((uint)outputBufferSize);

        // Create uniform for output buffer
        var outputUniform = GetUniform(outputRid, 0, RenderingDevice.UniformType.StorageBuffer);
        var uniformSet = GetRd().UniformSetCreate([outputUniform], cache.Shader, 0);

        // Encode push constants
        var pushConstantBytes = EncodeSingle(pushConstants);

        // Execute with push constants
        var computeList = GetRd().ComputeListBegin();
        GetRd().ComputeListBindComputePipeline(computeList, cache.Pipeline);
        GetRd().ComputeListBindUniformSet(computeList, uniformSet, 0);
        GetRd().ComputeListSetPushConstant(computeList, pushConstantBytes, (uint)pushConstantBytes.Length);
        GetRd().ComputeListDispatch(computeList, (uint)workGroupsX, (uint)workGroupsY, (uint)workGroupsZ);
        GetRd().ComputeListEnd();

        GetRd().Submit();
        GetRd().Sync();

        // Read output
        var outputBuffer = GetRd().BufferGetData(outputRid);
        var output = Decode<T>(outputBuffer);

        // Cleanup
        GetRd().FreeRid(uniformSet);
        GetRd().FreeRid(outputRid);

        return output;
    }

    /// <summary>
    /// Execute compute shader with push constants, input and output buffers
    /// Use when you need both parameters via push constants AND input data
    /// </summary>
    public T[] ExecuteComputeWithPushConstantsAndInput<T, TPushConstants>(
        string shaderName,
        TPushConstants pushConstants,
        T[] inputData,
        int outputSize,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1) where T : unmanaged where TPushConstants : unmanaged
    {
        if (!shaderCache.TryGetValue(shaderName, out var cache))
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        // Create input buffer
        var inputBuffer = Encode(inputData);
        var inputRid = GetRd().StorageBufferCreate((uint)inputBuffer.Length, inputBuffer);

        // Create output buffer
        var outputBufferSize = outputSize * System.Runtime.InteropServices.Marshal.SizeOf<T>();
        var outputRid = GetRd().StorageBufferCreate((uint)outputBufferSize);

        // Create uniforms
        var inputUniform = GetUniform(inputRid, 0, RenderingDevice.UniformType.StorageBuffer);
        var outputUniform = GetUniform(outputRid, 1, RenderingDevice.UniformType.StorageBuffer);
        var uniformSet = GetRd().UniformSetCreate([inputUniform, outputUniform], cache.Shader, 0);

        // Encode push constants
        var pushConstantBytes = EncodeSingle(pushConstants);

        // Execute
        var computeList = GetRd().ComputeListBegin();
        GetRd().ComputeListBindComputePipeline(computeList, cache.Pipeline);
        GetRd().ComputeListBindUniformSet(computeList, uniformSet, 0);
        GetRd().ComputeListSetPushConstant(computeList, pushConstantBytes, (uint)pushConstantBytes.Length);
        GetRd().ComputeListDispatch(computeList, (uint)workGroupsX, (uint)workGroupsY, (uint)workGroupsZ);
        GetRd().ComputeListEnd();

        GetRd().Submit();
        GetRd().Sync();

        // Read output
        var outputBuffer = GetRd().BufferGetData(outputRid);
        var output = Decode<T>(outputBuffer);

        // Cleanup
        GetRd().FreeRid(uniformSet);
        GetRd().FreeRid(inputRid);
        GetRd().FreeRid(outputRid);

        return output;
    }

    /// <summary>
    /// Execute compute shader asynchronously (for long-running operations)
    /// Does NOT block - returns immediately
    /// Use ReadComputeResultAsync to get results when ready
    /// </summary>
    public ComputeJob<T> ExecuteComputeAsync<T>(
        string shaderName,
        T[] inputData,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1) where T : unmanaged
    {
        if (!shaderCache.TryGetValue(shaderName, out var cache))
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        var buffer = Encode(inputData);
        var bufferRid = GetRd().StorageBufferCreate((uint)buffer.Length, buffer);

        var uniform = GetUniform(bufferRid, 0, RenderingDevice.UniformType.StorageBuffer);

        var uniformSet = GetRd().UniformSetCreate([uniform], cache.Shader, 0);

        var computeList = GetRd().ComputeListBegin();
        GetRd().ComputeListBindComputePipeline(computeList, cache.Pipeline);
        GetRd().ComputeListBindUniformSet(computeList, uniformSet, 0);
        GetRd().ComputeListDispatch(computeList, (uint)workGroupsX, (uint)workGroupsY, (uint)workGroupsZ);
        GetRd().ComputeListEnd();

        GetRd().Submit();
        // Note: No Sync() here - returns immediately!

        return new ComputeJob<T>
        {
            BufferRid = bufferRid,
            UniformSet = uniformSet,
            IsComplete = false
        };
    }

    /// <summary>
    /// Check if async compute job is complete and read results
    /// </summary>
    public bool TryReadComputeResult<T>(ComputeJob<T> job, out T[] result) where T : unmanaged
    {
        result = null;

        if (job == null || job.IsComplete)
            return false;

        // Check if GPU work is complete (non-blocking check would be ideal, but Godot doesn't expose this)
        // For now, we sync - in production you'd want to poll or use fences
        GetRd().Sync();

        var outputBuffer = GetRd().BufferGetData(job.BufferRid);
        result = Decode<T>(outputBuffer);

        // Cleanup
        GetRd().FreeRid(job.UniformSet);
        GetRd().FreeRid(job.BufferRid);
        job.IsComplete = true;

        return true;
    }

    /// <summary>
    /// Batch execute multiple compute operations with minimal overhead
    /// All operations use the same shader
    /// </summary>
    public List<T[]> ExecuteComputeBatch<T>(
        string shaderName,
        List<T[]> inputBatches,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1) where T : unmanaged
    {
        if (!shaderCache.TryGetValue(shaderName, out var cache))
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        var results = new List<T[]>();

        foreach (var inputData in inputBatches)
        {
            var result = ExecuteCompute(shaderName, inputData, workGroupsX, workGroupsY, workGroupsZ, reuseBuffer: true);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Batch execute with push constants - OPTIMAL for voxel generation
    /// Processes multiple chunks with different parameters efficiently
    /// </summary>
    public List<T[]> ExecuteComputeBatchWithPushConstants<T, TPushConstants>(
        string shaderName,
        List<TPushConstants> pushConstantsBatch,
        int outputSize,
        int workGroupsX,
        int workGroupsY = 1,
        int workGroupsZ = 1) where T : unmanaged where TPushConstants : unmanaged
    {
        if (!shaderCache.TryGetValue(shaderName, out var cache))
        {
            Debug.LogError($"Shader not loaded: {shaderName}");
            return null;
        }

        var results = new List<T[]>();
        var outputBufferSize = outputSize * System.Runtime.InteropServices.Marshal.SizeOf<T>();

        // Reuse output buffer for all batches
        var outputRid = GetRd().StorageBufferCreate((uint)outputBufferSize);
        var outputUniform = GetUniform(outputRid, 0, RenderingDevice.UniformType.StorageBuffer);
        var uniformSet = GetRd().UniformSetCreate([outputUniform], cache.Shader, 0);

        foreach (var pushConstants in pushConstantsBatch)
        {
            var pushConstantBytes = EncodeSingle(pushConstants);

            // Execute
            var computeList = GetRd().ComputeListBegin();
            GetRd().ComputeListBindComputePipeline(computeList, cache.Pipeline);
            GetRd().ComputeListBindUniformSet(computeList, uniformSet, 0);
            GetRd().ComputeListSetPushConstant(computeList, pushConstantBytes, (uint)pushConstantBytes.Length);
            GetRd().ComputeListDispatch(computeList, (uint)workGroupsX, (uint)workGroupsY, (uint)workGroupsZ);
            GetRd().ComputeListEnd();

            GetRd().Submit();
            GetRd().Sync();

            // Read output
            var outputBuffer = GetRd().BufferGetData(outputRid);
            var output = Decode<T>(outputBuffer);
            results.Add(output);
        }

        // Cleanup
        GetRd().FreeRid(uniformSet);
        GetRd().FreeRid(outputRid);

        return results;
    }

    #region Buffer Pooling

    private bool TryGetPooledBuffer(int size, out Rid buffer)
    {
        if (bufferPools.TryGetValue(size, out var pool) && pool.Count > 0)
        {
            buffer = pool.Dequeue();
            return true;
        }

        buffer = default;
        return false;
    }

    private void ReturnBufferToPool(int size, Rid buffer)
    {
        if (!bufferPools.ContainsKey(size))
        {
            bufferPools[size] = new Queue<Rid>();
        }

        bufferPools[size].Enqueue(buffer);
    }

    private void CleanupBufferPools()
    {
        foreach (var pool in bufferPools.Values)
        {
            while (pool.Count > 0)
            {
                GetRd().FreeRid(pool.Dequeue());
            }
        }
        bufferPools.Clear();
    }

    #endregion

    #region Encoding/Decoding - Optimized for unmanaged types

    private static byte[] Encode<T>(T[] input)
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);

        for (var i = 0; i < input.Length; i++)
        {
            writer.Write(input[i].Encode());
        }

        return memory.ToArray();
    }

    private static byte[] EncodeSingle<T>(T input) where T : unmanaged
    {
        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);
        writer.Write(input.Encode());
        return memory.ToArray();
    }

    private static T[] Decode<T>(byte[] input)
    {
        using var memory = new MemoryStream(input);
        using var reader = new BinaryReader(memory);

        var output = new List<T>();
        while (true)
        {
            if (memory.Position >= memory.Length || reader.TryDecode<T>(out var value) == false) break;

            output.Add(value);
        }

        return [.. output];
    }

    #endregion

    #region Cleanup

    private void CleanupShaderCache()
    {
        foreach (var cache in shaderCache.Values)
        {
            GetRd().FreeRid(cache.Pipeline);
            GetRd().FreeRid(cache.Shader);
        }
        shaderCache.Clear();
    }

    #endregion

    private class ComputeShaderCache
    {
        public Rid Shader;
        public Rid Pipeline;
    }
}

/// <summary>
/// Represents an asynchronous compute job
/// </summary>
public class ComputeJob<T>
{
    public Rid BufferRid;
    public Rid UniformSet;
    public bool IsComplete;
}

#endif