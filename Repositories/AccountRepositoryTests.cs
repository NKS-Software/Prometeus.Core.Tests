using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Repositories;
using Prometeus.Core.Generators;
using Prometeus.Core.Tests.Utils;

namespace Prometeus.Core.Tests.Repositories
{
    public class AccountRepositoryTests(DepedencyFixture depedencyFixture) : IClassFixture<DepedencyFixture>
    {
        private readonly AccountRepository accountRepository = depedencyFixture.DatabaseContext.Get<AccountRepository>();
        private readonly ClientRepository clientRepository = depedencyFixture.DatabaseContext.Get<ClientRepository>();

        #region IsExistsAsync
        [Fact]
        public async Task IsExistsAsync_WithNotExistAccount()
        {
            var n = AccountNumberGenerator.Generate(408, 02, 643, DataGenerators.BIK, Random.Shared.Next(0, 10000));
            var res = await accountRepository.IsExistsAsync(n);
            Assert.False(res);
        }

        [Fact]
        public async Task IsExistsAsync_WithInvalidNumber()
        {
            var res = await accountRepository.IsExistsAsync("40802643300000000000");
            Assert.False(res);
        }

        [Fact]
        public async Task IsExistsAsync_WithExistAccount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.IsExistsAsync(account.Number);
            Assert.True(res);
        }
        #endregion

        #region PossibleCreditAsync
        [Fact]
        public async Task PossibleCreditAsync_WithNotExists()
        {
            var n = AccountNumberGenerator.Generate(408,02,613,DataGenerators.BIK,100);
            var res = await accountRepository.PossibleCreditAsync(n);
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleCreditAsync_WithInvalidNumber()
        {
            var res = await accountRepository.PossibleCreditAsync("40802643300000000000");
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleCreditAsync_WithExistAccount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.PossibleCreditAsync(account.Number);
            Assert.True(res);
        }

        [Fact]
        public async Task PossibleCreditAsync_WithClosedAccount()
        {
            var account = await CreateAccountAsync(0);
            await accountRepository.CloseAsync(account.Number);
            var res = await accountRepository.PossibleCreditAsync(account.Number);
            Assert.False(res);
        }
        #endregion

        #region PossibleDebitAsync
        [Fact]
        public async Task PossibleDebitAsync_WithNotExists()
        {
            var n = AccountNumberGenerator.Generate(408, 02, 613, DataGenerators.BIK, 100);
            var res = await accountRepository.PossibleDebitAsync(n, 100);
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleDebitAsync_WithInvalidNumber()
        {
            var res = await accountRepository.PossibleDebitAsync("40802643300000000000", 100);
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleDebitAsync_WithExistAccount()
        {
            var account = await CreateAccountAsync(1000);
            var res = await accountRepository.PossibleDebitAsync(account.Number,100);
            Assert.True(res);
        }

        [Fact]
        public async Task PossibleDebitAsync_WithNegativeAmount()
        {
            var account = await CreateAccountAsync(1000);
            var res = await accountRepository.PossibleDebitAsync(account.Number, -100);
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleDebitAsync_WithZeroAmount()
        {
            var account = await CreateAccountAsync(1000);
            var res = await accountRepository.PossibleDebitAsync(account.Number, 0);
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleDebitAsync_WithLessAmount()
        {
            var account = await CreateAccountAsync(100);
            var res = await accountRepository.PossibleDebitAsync(account.Number, 1000);
            Assert.False(res);
        }

        [Fact]
        public async Task PossibleDebitAsync_WithClosedAccount()
        {
            var account = await CreateAccountAsync(0);
            await accountRepository.CloseAsync(account.Number);
            var res = await accountRepository.PossibleDebitAsync(account.Number,100);
            Assert.False(res);
        }
        #endregion

        #region BelongsToClientAsync
        [Fact]
        public async Task BelongsToClient_WithExistClient()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            var res = await accountRepository.BelongsToClientAsync(account.Number, client.Id);
            Assert.True(res);
        }

        [Fact]
        public async Task BelongsToClient_WithNotExistClient()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            var res = await accountRepository.BelongsToClientAsync(account.Number, Guid.CreateVersion7());
            Assert.False(res);
        }

        [Fact]
        public async Task BelongsToClient_WithNotExistAccount()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var accountNumber = AccountNumberGenerator.Generate(408, 01, 613, DataGenerators.BIK);
            var res = await accountRepository.BelongsToClientAsync(accountNumber, client.Id);
            Assert.False(res);
        }
        #endregion

        #region GetClientByNumberAsync
        [Fact]
        public async Task GetClientByNumberAsync_WithExistAccount()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            var clientId = await accountRepository.GetClientByNumberAsync(account.Number);
            Assert.Equal(client.Id, clientId);
        }

        [Fact]
        public async Task GetClientByNumberAsync_WithNotExistAccount()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var accountNumber = AccountNumberGenerator.Generate(408, 01, 613, DataGenerators.BIK);
            var clientId = await accountRepository.GetClientByNumberAsync(accountNumber);
            Assert.Null(clientId);
        }
        #endregion

        #region GetByNumberAsync
        [Fact]
        public async Task GetByNumberAsync_WithExistsAccount()
        {
            var account = await CreateAccountAsync(1000);
            var a = await accountRepository.GetByNumberAsync(account.Number);
            Assert.NotNull(a);
            Assert.True(a.Balance > 0);
        }

        [Fact]
        public async Task GetByNumberAsync_WithNotExistsAccount()
        {
            var an = AccountNumberGenerator.Generate(407, 01, 613, DataGenerators.BIK, 9999);
            var account = await accountRepository.GetByNumberAsync(an);
            Assert.Null(account);
        }
        #endregion

        #region GetByClientId
        [Fact]
        public async Task GetByClientId_WithExistClient()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            for(int i = 0; i < 10; i++)
            {
                var account = DataGenerators.CreateRandomAccount(client.Id);
                account = await accountRepository.OpenAsync(account);
                Assert.NotNull(account);
            }
            var accounts = await accountRepository.GetByClientIdAsync(client.Id);
            Assert.Equal(10, accounts.Count);
        }

        [Fact]
        public async Task GetByClientId_WithNotExistClient()
        {
            var accounts = await accountRepository.GetByClientIdAsync(Guid.CreateVersion7());
            Assert.Empty(accounts);
        }
        #endregion

        #region OpenAsync
        [Fact]
        public async Task OpenAsync_WithExistClient()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
        }

        [Fact]
        public async Task OpenAsync_WithNotExistClient()
        {
            var account = DataGenerators.CreateRandomAccount(Guid.CreateVersion7());
            account = await accountRepository.OpenAsync(account);
            Assert.Null(account);
        }
        #endregion

        #region RenameAsync
        [Fact]
        public async Task RenameAsync_WithExistedAccount()
        {
            var account = await CreateAccountAsync();
            string? newName = await accountRepository.RenameAsync(account.Number, "Test account 2");
            Assert.NotNull(newName);
            Assert.Equal("Test account 2", newName);
        }

        [Fact]
        public async Task RenameAsync_WithClosedAccount()
        {
            var account = await CreateAccountAsync();
            await accountRepository.CloseAsync(account.Number);
            string? newName = await accountRepository.RenameAsync(account.Number, "Test account 2");
            Assert.Null(newName);
        }

        [Fact]
        public async Task RenameAsync_WithNotExistedAccount()
        {
            var account = DataGenerators.CreateRandomAccount(Guid.CreateVersion7());
            string? newName = await accountRepository.RenameAsync(account.Number, "Test account 2");
            Assert.Null(newName);
        }
        #endregion

        #region CloseAsync
        [Fact]
        public async Task CloseAsyncByNumber_WithExistAccount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.CloseAsync(account.Number);
            Assert.True(res);
        }

        [Fact]
        public async Task CloseAsyncByNumber_WithNotExistAccount()
        {
            var accountNumber = AccountNumberGenerator.Generate(408,01,613,DataGenerators.BIK,0001);
            var res = await accountRepository.CloseAsync(accountNumber);
            Assert.False(res);
        }

        [Fact]
        public async Task CloseAsyncByNumber_WithClosedAccount()
        {
            var account = await CreateAccountAsync();
            await accountRepository.CloseAsync(account.Number);
            var res = await accountRepository.CloseAsync(account.Number);
            Assert.False(res);
        }
        #endregion

        #region DebitAsync
        [Theory]
        [InlineData(1000, 100)]
        public async Task DebitAsync_WithExistAccount(decimal balance, decimal amount)
        {
            var account = await CreateAccountAsync(balance);
            await accountRepository.DebitAsync(account.Number, amount);
            account = await accountRepository.GetByNumberAsync(account.Number);
            Assert.NotNull(account);
            Assert.Equal(balance - amount, account.Balance);
        }

        [Theory]
        [InlineData(100,1000)]
        [InlineData(500, 1000)]
        public async Task DebitAsync_WithAmountGreatBalance(decimal balance, decimal amount)
        {
            var account = await CreateAccountAsync(balance);
            await accountRepository.DebitAsync(account.Number, amount);
            account = await accountRepository.GetByNumberAsync(account.Number);
            Assert.NotNull(account);
            Assert.Equal(balance, account.Balance);
        }

        [Fact]
        public async Task DebitAsync_WithNotExistAccount()
        {
            var accountNumber = AccountNumberGenerator.Generate(408, 02, 613, DataGenerators.BIK);
            var res = await accountRepository.DebitAsync(accountNumber, 100);
            Assert.False(res);
        }


        [Fact]
        public async Task DebitAsync_WithClosedAccount()
        {
            var account = await CreateAccountAsync();
            await accountRepository.CloseAsync(account.Number);
            var res = await accountRepository.DebitAsync(account.Number, 100);
            Assert.False(res);
        }

        [Fact]
        public async Task DebitAsync_WithNegativeAmount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.DebitAsync(account.Number, -100);
            Assert.False(res);
        }

        [Fact]
        public async Task DebitAsync_WithZeroAmount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.DebitAsync(account.Number, 0);
            Assert.False(res);
        }
        #endregion

        #region CreditAsync
        [Theory]
        [InlineData(1000,100)]
        public async Task CreditAsync_WithExistAccount(decimal balance, decimal amount)
        {
            var account = await CreateAccountAsync(balance);
            await accountRepository.CreditAsync(account.Number, amount);
            account = await accountRepository.GetByNumberAsync(account.Number);
            Assert.NotNull(account);
            Assert.Equal(balance + amount, account.Balance);
        }

        [Fact]
        public async Task CreditAsync_WithNotExistAccount()
        {
            var accountNumber = AccountNumberGenerator.Generate(408, 02, 613, DataGenerators.BIK);
            var res = await accountRepository.CreditAsync(accountNumber, 100);
            Assert.False(res);
        }

        [Fact]
        public async Task CreditAsync_WithClosedAccount()
        {
            var account = await CreateAccountAsync();
            await accountRepository.CloseAsync(account.Number);
            var res = await accountRepository.CreditAsync(account.Number, 100);
            Assert.False(res);
        }

        [Fact]
        public async Task CreditAsync_WithNegativeAmount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.CreditAsync(account.Number, -100);
            Assert.False(res);
        }

        [Fact]
        public async Task CreditAsync_WithZeroAmount()
        {
            var account = await CreateAccountAsync();
            var res = await accountRepository.CreditAsync(account.Number, 0);
            Assert.False(res);
        }
        #endregion

        #region Utils
        private async Task<Account> CreateAccountAsync(decimal balance = 0)
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            if (balance > 0)
            {
                var res = await accountRepository.CreditAsync(account.Number, balance);
                Assert.True(res);
                account.Balance = balance;
            }

            return account;
        }
        #endregion
    }
}
