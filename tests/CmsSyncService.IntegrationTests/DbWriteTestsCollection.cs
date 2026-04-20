using Xunit;

namespace CmsSyncService.Api.Tests;

[CollectionDefinition("DbWriteTests", DisableParallelization = true)]
public class DbWriteTestsCollection : ICollectionFixture<object> { }
