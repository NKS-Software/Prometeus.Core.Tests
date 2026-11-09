using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Repositories;
using Prometeus.Core.Generators;
using Prometeus.Core.Tests.Utils;

namespace Prometeus.Core.Tests.Repositories;

public class TransactionRepositoryTests(DepedencyFixture depedencyFixture) : IClassFixture<DepedencyFixture>
{
    private readonly ClientRepository clientRepository = depedencyFixture.DatabaseContext.Get<ClientRepository>();
    private readonly AccountRepository accountRepository = depedencyFixture.DatabaseContext.Get<AccountRepository>();
    private readonly TransactionRepository transactionRepository = depedencyFixture.DatabaseContext.Get<TransactionRepository>();

    #region IsExistsAsync
    [Fact]
    public async Task IsExistsAsync_WithNotExists()
    {
        var res = await transactionRepository.IsExistsAsync(Guid.NewGuid());
        Assert.False(res);
    }

    [Fact]
    public async Task IsExistsAsync_ForExistTransaction()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.IsExistsAsync(transaction.Id);
        Assert.True(res);
    }
    #endregion

    #region IsCompletedAsync
    [Fact]
    public async Task IsCompletedAsync_WithNotExists()
    {
        var res = await transactionRepository.IsCompletedAsync(Guid.NewGuid());
        Assert.False(res);
    }

    [Fact]
    public async Task IsCompletedAsync_ForPendingTransaction()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.IsCompletedAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task IsCompletedAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.IsCompletedAsync(transaction.Id);
        Assert.True(res);
    }

    [Fact]
    public async Task IsCompletedAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.IsCompletedAsync(transaction.Id);
        Assert.True(res);
    }
    #endregion

    #region IsRejectedAsync
    [Fact]
    public async Task IsRejectedAsync_WithNotExists()
    {
        var res = await transactionRepository.IsRejectedAsync(Guid.NewGuid());
        Assert.False(res);
    }

    [Fact]
    public async Task IsRejectedAsync_ForPendingTransaction()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.IsRejectedAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task IsRejectedAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.IsRejectedAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task IsRejectedAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.IsRejectedAsync(transaction.Id);
        Assert.True(res);
    }
    #endregion

    #region CanCompleteAsync
    [Fact]
    public async Task CanCompleteAsync_WithNotExists()
    {
        var res = await transactionRepository.CanCompleteAsync(Guid.NewGuid());
        Assert.False(res);
    }

    [Fact]
    public async Task CanCompleteAsync_WithoutEntries()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.CanCompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CanCompleteAsync_WithOneEntry()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        var res = await transactionRepository.CanCompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CanCompleteAsync_WithUnbalancedEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 500);
        var res = await transactionRepository.CanCompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CanCompleteAsync_WithBalancedEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 1000);
        var res = await transactionRepository.CanCompleteAsync(transaction.Id);
        Assert.True(res);
    }

    [Fact]
    public async Task CanCompleteAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.CanCompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CanCompleteAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.CanCompleteAsync(transaction.Id);
        Assert.False(res);
    }
    #endregion

    #region CreateAsync
    [Fact]
    public async Task CreateAsync_WithValidData()
    {
        var res = await transactionRepository.CreateAsync();
        Assert.NotNull(res);
    }
    #endregion

    #region CreateEntryAsync
    [Fact]
    public async Task CreateEntryAsync_WithValidData()
    {
        var transaction = await CreateTransactionAsync();
        var entry = new TransactionEntry
        {
            TransactionId = transaction.Id,
            AccountNumber = await CreateAccountAsync(),
            Amount = 1000,
            Type = EntryType.Source
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.NotNull(res);
    }

    [Fact]
    public async Task CreateEntryAsync_WithNotExistsTransaction()
    {
        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            AccountNumber = await CreateAccountAsync(),
            Amount = 1000,
            Type = EntryType.Source
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.Null(res);
    }

    [Fact]
    public async Task CreateEntryAsync_WithNotExistsAccount()
    {
        var transaction = await CreateTransactionAsync();
        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            AccountNumber = AccountNumberGenerator.Generate(408, 02, 810, DataGenerators.BIK),
            Amount = 1000,
            Type = EntryType.Source
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.Null(res);
    }

    [Fact]
    public async Task CreateEntryAsync_WithClosedAccount()
    {
        var transaction = await CreateTransactionAsync();
        var accountNumber = await CreateAccountAsync();
        var account = await accountRepository.GetByNumberAsync(accountNumber);
        Assert.NotNull(account);
        await accountRepository.CloseAsync(account.Number);

        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            AccountNumber = accountNumber,
            Amount = 1000,
            Type = EntryType.Source
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.Null(res);
    }

    [Fact]
    public async Task CreateEntryAsync_WithZeroAmount()
    {
        var transaction = await CreateTransactionAsync();
        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            AccountNumber = await CreateAccountAsync(),
            Amount = 0,
            Type = EntryType.Source
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.Null(res);
    }

    [Fact]
    public async Task CreateEntryAsync_WithNegativeAmount()
    {
        var transaction = await CreateTransactionAsync();
        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            AccountNumber = await CreateAccountAsync(),
            Amount = -1000,
            Type = EntryType.Source
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.Null(res);
    }
    #endregion

    #region GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_WithNotExists()
    {
        var res = await transactionRepository.GetByIdAsync(Guid.NewGuid());
        Assert.Null(res);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutEntries()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.GetByIdAsync(transaction.Id);
        Assert.NotNull(res);
        Assert.Empty(res.Entries);
    }

    [Fact]
    public async Task GetByIdAsync_WithEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 1000);
        var res = await transactionRepository.GetByIdAsync(transaction.Id);
        Assert.NotNull(res);
        Assert.Equal(2, res.Entries.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithMetadata()
    {
        var transaction = await CreateTransactionAsync();
        var metadata = new Dictionary<string, string> { { "key", "value" } };
        await transactionRepository.UpdateMetadataAsync(transaction.Id, metadata);
        var res = await transactionRepository.GetByIdAsync(transaction.Id);
        Assert.NotNull(res);
        Assert.Equal("value", res.Metadata["key"]);
    }
    #endregion

    #region GetAllForAccountAsync
    [Fact]
    public async Task GetAllForAccountAsync_WithNotExistsAccount()
    {
        var res = await transactionRepository.GetAllForAccountAsync(
            AccountNumberGenerator.Generate(408, 02, 810, DataGenerators.BIK), 10, 1);
        Assert.Empty(res);
    }

    [Fact]
    public async Task GetAllForAccountAsync_WithExistsAccount()
    {
        var accountNumber = await CreateAccountAsync();
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000, accountNumber);
        var res = await transactionRepository.GetAllForAccountAsync(accountNumber, 10, 1);
        Assert.Single(res);
    }

    [Fact]
    public async Task GetAllForAccountAsync_WithPagination()
    {
        var accountNumber = await CreateAccountAsync();
        for (int i = 0; i < 5; i++)
        {
            var transaction = await CreateTransactionAsync();
            await CreateEntryAsync(transaction.Id, EntryType.Source, 1000, accountNumber);
        }
        var page1 = await transactionRepository.GetAllForAccountAsync(accountNumber, 2, 1);
        var page2 = await transactionRepository.GetAllForAccountAsync(accountNumber, 2, 2);
        var page3 = await transactionRepository.GetAllForAccountAsync(accountNumber, 2, 3);
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.Single(page3);
    }
    #endregion

    #region GetAllForClientAsync
    [Fact]
    public async Task GetAllForClientAsync_WithNotExistsClient()
    {
        var res = await transactionRepository.GetAllForClientAsync(Guid.NewGuid(), 10, 1);
        Assert.Empty(res);
    }

    [Fact]
    public async Task GetAllForClientAsync_WithExistsClient()
    {
        var accountNumber = await CreateAccountAsync();
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000, accountNumber);
        var account = await accountRepository.GetByNumberAsync(accountNumber);
        Assert.NotNull(account);
        var res = await transactionRepository.GetAllForClientAsync(account.ClientId, 10, 1);
        Assert.Single(res);
    }
    #endregion

    #region GetMetadataByIdAsync
    [Fact]
    public async Task GetMetadataByIdAsync_WithNotExists()
    {
        var res = await transactionRepository.GetMetadataByIdAsync(Guid.NewGuid());
        Assert.Null(res);
    }

    [Fact]
    public async Task GetMetadataByIdAsync_WithoutMetadata()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.GetMetadataByIdAsync(transaction.Id);
        Assert.NotNull(res);
        Assert.Empty(res);
    }

    [Fact]
    public async Task GetMetadataByIdAsync_WithMetadata()
    {
        var transaction = await CreateTransactionAsync();
        var metadata = new Dictionary<string, string> { { "key", "value" } };
        await transactionRepository.UpdateMetadataAsync(transaction.Id, metadata);
        var res = await transactionRepository.GetMetadataByIdAsync(transaction.Id);
        Assert.NotNull(res);
        Assert.Equal("value", res["key"]);
    }
    #endregion

    #region GetStatusByIdAsync
    [Fact]
    public async Task GetStatusByIdAsync_WithNotExists()
    {
        var res = await transactionRepository.GetStatusByIdAsync(Guid.NewGuid());
        Assert.Null(res);
    }

    [Fact]
    public async Task GetStatusByIdAsync_ForPendingTransaction()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.GetStatusByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Pending, res);
    }

    [Fact]
    public async Task GetStatusByIdAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.GetStatusByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Completed, res);
    }

    [Fact]
    public async Task GetStatusByIdAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.GetStatusByIdAsync(transaction.Id);
        Assert.Equal(TransactionStatus.Rejected, res);
    }
    #endregion

    #region UpdateMetadataAsync
    [Fact]
    public async Task UpdateMetadataAsync_WithNotExists()
    {
        var res = await transactionRepository.UpdateMetadataAsync(
            Guid.NewGuid(), new Dictionary<string, string> { { "key", "value" } });
        Assert.False(res);
    }

    [Fact]
    public async Task UpdateMetadataAsync_ForPendingTransaction()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.UpdateMetadataAsync(
            transaction.Id, new Dictionary<string, string> { { "key", "value" } });
        Assert.True(res);
    }

    [Fact]
    public async Task UpdateMetadataAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.UpdateMetadataAsync(
            transaction.Id, new Dictionary<string, string> { { "key", "value" } });
        Assert.False(res);
    }

    [Fact]
    public async Task UpdateMetadataAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.UpdateMetadataAsync(
            transaction.Id, new Dictionary<string, string> { { "key", "value" } });
        Assert.False(res);
    }
    #endregion

    #region CompleteAsync
    [Fact]
    public async Task CompleteAsync_WithNotExists()
    {
        var res = await transactionRepository.CompleteAsync(Guid.NewGuid());
        Assert.False(res);
    }

    [Fact]
    public async Task CompleteAsync_WithoutEntries()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.CompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CompleteAsync_WithUnbalancedEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 500);
        var res = await transactionRepository.CompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CompleteAsync_WithBalancedEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 1000);
        var res = await transactionRepository.CompleteAsync(transaction.Id);
        Assert.True(res);
    }

    [Fact]
    public async Task CompleteAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.CompleteAsync(transaction.Id);
        Assert.False(res);
    }

    [Fact]
    public async Task CompleteAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.CompleteAsync(transaction.Id);
        Assert.False(res);
    }
    #endregion

    #region RejectAsync
    [Fact]
    public async Task RejectAsync_WithNotExists()
    {
        var res = await transactionRepository.RejectAsync(Guid.NewGuid(), "reason");
        Assert.False(res);
    }

    [Fact]
    public async Task RejectAsync_WithEmptyReason()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.RejectAsync(transaction.Id, "");
        Assert.False(res);
    }

    [Fact]
    public async Task RejectAsync_WithoutEntries()
    {
        var transaction = await CreateTransactionAsync();
        var res = await transactionRepository.RejectAsync(transaction.Id, "reason");
        Assert.False(res);
    }

    [Fact]
    public async Task RejectAsync_WithUnbalancedEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 500);
        var res = await transactionRepository.RejectAsync(transaction.Id, "reason");
        Assert.False(res);
    }

    [Fact]
    public async Task RejectAsync_WithBalancedEntries()
    {
        var transaction = await CreateTransactionAsync();
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 1000);
        var res = await transactionRepository.RejectAsync(transaction.Id, "reason");
        Assert.True(res);
    }

    [Fact]
    public async Task RejectAsync_ForCompletedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await CompleteTransactionAsync(transaction);
        var res = await transactionRepository.RejectAsync(transaction.Id, "reason");
        Assert.False(res);
    }

    [Fact]
    public async Task RejectAsync_ForRejectedTransaction()
    {
        var transaction = await CreateTransactionAsync();
        await RejectTransactionAsync(transaction);
        var res = await transactionRepository.RejectAsync(transaction.Id, "reason2");
        Assert.False(res);
    }
    #endregion

    #region Utils
    private async Task<Transaction> CreateTransactionAsync()
    {
        var res = await transactionRepository.CreateAsync();
        Assert.NotNull(res);
        return res;
    }

    private async Task<TransactionEntry> CreateEntryAsync(Guid transactionId, EntryType type, decimal amount, string? accountNumber = null)
    {
        accountNumber ??= await CreateAccountAsync();
        var entry = new TransactionEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            AccountNumber = accountNumber,
            Amount = amount,
            Type = type
        };
        var res = await transactionRepository.CreateEntryAsync(entry);
        Assert.NotNull(res);
        return res;
    }

    private async Task<string> CreateAccountAsync()
    {
        var client = DataGenerators.CreateRandomClient();
        var addedClient = await clientRepository.AddAsync(client);
        Assert.NotNull(addedClient);
        var account = DataGenerators.CreateRandomAccount(addedClient.Id);
        var addedAccount = await accountRepository.OpenAsync(account);
        Assert.NotNull(addedAccount);
        return addedAccount.Number;
    }

    private async Task CompleteTransactionAsync(Transaction transaction)
    {
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 1000);
        var res = await transactionRepository.CompleteAsync(transaction.Id);
        Assert.True(res);
    }

    private async Task RejectTransactionAsync(Transaction transaction)
    {
        await CreateEntryAsync(transaction.Id, EntryType.Source, 1000);
        await CreateEntryAsync(transaction.Id, EntryType.Destination, 1000);
        var res = await transactionRepository.RejectAsync(transaction.Id, "rejected");
        Assert.True(res);
    }
    #endregion
}