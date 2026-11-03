using Prometeus.Core.Data.Context;
using Prometeus.Core.Data.Queries.DDL;
using Prometeus.Core.Data.Repositories;
using Prometeus.Core.Storage;

namespace Prometeus.Core.Tests.Utils;

public class DepedencyFixture
{
    public DatabaseContext DatabaseContext { get; private set; }
    public KeyStore KeyStore { get; private set; }
    public DepedencyFixture()
    {
        DatabaseContext = new DatabaseContext.Builder()
            .InMemory()
            .AddInitializationQuery(Tables.Clients)
            .AddInitializationQuery(Tables.Accounts)
            .AddInitializationQuery(Tables.Cards)
            .AddInitializationQuery(Tables.Transactions)
            .AddInitializationQuery(Triggers.AccountOnAfterClosed)
            .AddInitializationQuery(Indexes.Accounts)
            .Add(conn => new ClientRepository(conn))
            .Add(conn => new AccountRepository(conn))
            .Add(conn => new CardRepository(conn))
            .Add(conn => new TransactionRepository(conn))
            .Build();
        DatabaseContext.EnsureCreateAsync().GetAwaiter().GetResult();

        KeyStore = new KeyStore.Builder()
            .InMemory()
            .SetMasterKey("0123456789abcdef0123456789abcdef")
            .Build();
        KeyStore.EnsureCreate();
    }
}
