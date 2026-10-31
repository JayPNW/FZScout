using FZScout.Site;

namespace FZScout.Tests.Site;

// Shared tests for all implementations of IApiKeyStore
public abstract class ApiKeyStoreContract
{
    private protected readonly IApiKeyStore Store;

    /// <param name="emptyStore">A store with no key saved yet. xUnit makes a new one for each test.</param>
    private protected ApiKeyStoreContract(IApiKeyStore emptyStore)
    {
        this.Store = emptyStore;
    }

    [Fact]
    public void LoadsNothingBeforeASave()
    {
        Assert.Null(this.Store.Load());
    }

    [Fact]
    public void LoadsTheSavedKey()
    {
        this.Store.Save(new ApiKey("fz_live_saved"));

        Assert.Equal(new ApiKey("fz_live_saved"), this.Store.Load());
    }

    [Fact]
    public void LoadsTheLastSavedKey()
    {
        this.Store.Save(new ApiKey("fz_live_first"));
        this.Store.Save(new ApiKey("fz_live_second"));

        Assert.Equal(new ApiKey("fz_live_second"), this.Store.Load());
    }

    [Fact]
    public void LoadsNothingAfterADelete()
    {
        this.Store.Save(new ApiKey("fz_live_saved"));

        this.Store.Delete();

        Assert.Null(this.Store.Load());
    }

    [Fact]
    public void DeletingWithNothingSavedIsFine()
    {
        this.Store.Delete();

        Assert.Null(this.Store.Load());
    }

    [Fact]
    public void SaysWhereTheKeyIsKept()
    {
        Assert.False(string.IsNullOrWhiteSpace(this.Store.StorageLocationForDisplay));
    }
}
