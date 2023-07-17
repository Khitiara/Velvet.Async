namespace Velvet.Async;

public readonly struct AsyncUnit : IEquatable<AsyncUnit>
{
    public static readonly AsyncUnit Default = default;

    public override int GetHashCode() => 0;

    public bool Equals(AsyncUnit other) => true;

    public override string ToString() => "()";

    public override bool Equals(object? obj) => obj is AsyncUnit unit && Equals(unit);

    public static bool operator ==(AsyncUnit left, AsyncUnit right) => left.Equals(right);

    public static bool operator !=(AsyncUnit left, AsyncUnit right) => !(left == right);
}