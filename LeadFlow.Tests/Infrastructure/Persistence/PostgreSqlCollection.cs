using Xunit;

namespace LeadFlow.Tests.Infrastructure.Persistence;

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlDatabaseFixture>
{
    public const string Name = "PostgreSQL database";
}
