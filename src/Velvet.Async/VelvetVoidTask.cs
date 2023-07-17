using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Velvet.Async.CompilerServices;

namespace Velvet.Async;

/// <summary>
/// A more efficient alternative to <c>async void</c> in Velvet contexts.
/// Call <see cref="Forget"/> to dismiss the warning.
/// </summary>
[AsyncMethodBuilder(typeof(VelvetVoidTaskAsyncMethodBuilder))]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public struct VelvetVoidTask
{
    public void Forget() { }
}