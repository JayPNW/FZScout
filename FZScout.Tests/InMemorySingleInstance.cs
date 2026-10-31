namespace FZScout.Tests;

/// <summary>Like <see cref="SingleInstance"/>, but only among the apps in this test that share it.</summary>
class InMemorySingleInstance
{
    bool _isClaimed;

    public IDisposable? TryClaim()
    {
        if (this._isClaimed)
        {
            return null;
        }

        this._isClaimed = true;
        return new Claim(this);
    }

    class Claim(InMemorySingleInstance singleInstance) : IDisposable
    {
        public void Dispose() => singleInstance._isClaimed = false;
    }
}
