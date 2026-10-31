namespace FZScout;

/// <summary>A site's bonus type key, like <c>experience</c> or <c>none</c>.</summary>
readonly record struct BonusKey(string Value)
{
    public static readonly BonusKey None = new("none");

    public static readonly BonusKey Unconfirmed = new("unconfirmed");

    public override string ToString() => Value;
}
