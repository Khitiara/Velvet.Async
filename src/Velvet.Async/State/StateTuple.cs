using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Velvet.Async.State;

internal static class StateTuple
{
    public static StateTuple<T1> Create<T1>(T1 item1) {
        return StatePool<T1>.Create(item1);
    }

    public static StateTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) {
        return StatePool<T1, T2>.Create(item1, item2);
    }

    public static StateTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) {
        return StatePool<T1, T2, T3>.Create(item1, item2, item3);
    }
}

internal class StateTuple<T1> : IDisposable
{
    public T1 Item1 = default!;

    public void Deconstruct(out T1 item1) {
        item1 = this.Item1;
    }

    public void Dispose() {
        StatePool<T1>.Return(this);
    }
}

internal static class StatePool<T1>
{
    private static readonly ObjectPool<StateTuple<T1>> queue = new(() => new StateTuple<T1>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1> Create(T1 item1) {
        StateTuple<T1> value = queue.Allocate();
        value.Item1 = item1;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T1> tuple) {
        tuple.Item1 = default!;
        queue.Free(tuple);
    }
}

internal class StateTuple<T1, T2> : IDisposable
{
    public T1 Item1 = default!;
    public T2 Item2 = default!;

    public void Deconstruct(out T1 item1, out T2 item2) {
        item1 = this.Item1;
        item2 = this.Item2;
    }

    public void Dispose() {
        StatePool<T1, T2>.Return(this);
    }
}

internal static class StatePool<T1, T2>
{
    private static readonly ConcurrentQueue<StateTuple<T1, T2>> queue = new ConcurrentQueue<StateTuple<T1, T2>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1, T2> Create(T1 item1, T2 item2) {
        if (queue.TryDequeue(out var value)) {
            value.Item1 = item1;
            value.Item2 = item2;
            return value;
        }

        return new StateTuple<T1, T2> { Item1 = item1, Item2 = item2 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T1, T2> tuple) {
        tuple.Item1 = default!;
        tuple.Item2 = default!;
        queue.Enqueue(tuple);
    }
}

internal class StateTuple<T1, T2, T3> : IDisposable
{
    public T1 Item1 = default!;
    public T2 Item2 = default!;
    public T3 Item3 = default!;

    public void Deconstruct(out T1 item1, out T2 item2, out T3 item3) {
        item1 = this.Item1;
        item2 = this.Item2;
        item3 = this.Item3;
    }

    public void Dispose() {
        StatePool<T1, T2, T3>.Return(this);
    }
}

internal static class StatePool<T1, T2, T3>
{
    private static readonly ObjectPool<StateTuple<T1, T2, T3>> Queue = new(() => new StateTuple<T1, T2, T3>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StateTuple<T1, T2, T3> Create(T1 item1, T2 item2, T3 item3) {
        StateTuple<T1, T2, T3> value = Queue.Allocate();
        value.Item1 = item1;
        value.Item2 = item2;
        value.Item3 = item3;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StateTuple<T1, T2, T3> tuple) {
        tuple.Item1 = default!;
        tuple.Item2 = default!;
        tuple.Item3 = default!;
        Queue.Free(tuple);
    }
}