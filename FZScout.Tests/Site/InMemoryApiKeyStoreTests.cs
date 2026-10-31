using FZScout.Site;

namespace FZScout.Tests.Site;

public class InMemoryApiKeyStoreTests() : ApiKeyStoreContract(new InMemoryApiKeyStore());
