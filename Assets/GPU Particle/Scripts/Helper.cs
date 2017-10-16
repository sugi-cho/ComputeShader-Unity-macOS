using UnityEngine;
using System.Runtime.InteropServices;

public static class Helper
{
    public static ComputeBuffer CreateComputeBuffer<T>(int count, ComputeBufferType type = ComputeBufferType.Default)
    {
        return new ComputeBuffer(count, Marshal.SizeOf(typeof(T)), type);
    }

    public static ComputeBuffer CreateComputeBuffer<T>(T[] array, bool setData = false)
    {
        var buffer = CreateComputeBuffer<T>(array.Length);
        if (setData)
            buffer.SetData(array);
        return buffer;
    }
}