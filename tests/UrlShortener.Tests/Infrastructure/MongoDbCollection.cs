namespace UrlShortener.Tests.Infrastructure;

[CollectionDefinition(Name)]
public class MongoDbCollection : ICollectionFixture<MongoDbContainerFixture>
{
    public const string Name = "MongoDB";
}
