namespace Velvet.Async.CompilerServices;

internal interface IAsyncStateMachineBox
{
    void MoveNext();

    Action MoveNextAction { get; }

    void Return();
}