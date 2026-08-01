using LithoManager.IntegrationTests
    .Api.Infrastructure;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Collections;

[CollectionDefinition(
    Name,
    DisableParallelization = true)]
public sealed class AuthenticationDatabaseCollection
    : ICollectionFixture<
        AuthenticationDatabaseFixture>,
      ICollectionFixture<
        LithoManagerWebApplicationFactory>
{
    public const string Name =
        "Authentication database";
}