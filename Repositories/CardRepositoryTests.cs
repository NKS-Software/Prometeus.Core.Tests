using Prometeus.Core.Data.Entities;
using Prometeus.Core.Data.Repositories;
using Prometeus.Core.Generators;
using Prometeus.Core.Tests.Utils;
using Prometeus.Core.Utils;
using TimeMachine.Core;

namespace Prometeus.Core.Tests.Repositories
{
    public class CardRepositoryTests(DepedencyFixture depedencyFixture) : IClassFixture<DepedencyFixture>
    {
        private readonly ClientRepository clientRepository = depedencyFixture.DatabaseContext.Get<ClientRepository>();
        private readonly AccountRepository accountRepository = depedencyFixture.DatabaseContext.Get<AccountRepository>();
        private readonly CardRepository cardRepository = depedencyFixture.DatabaseContext.Get<CardRepository>();

        #region IsExistsAsync
        [Fact]
        public async Task IsExistsAsync_WithNotExists()
        {
            var cn = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.IsExistsAsync(cn);
            Assert.False(res);
        }

        [Fact]
        public async Task IsExistsAsync_ForExistCard()
        {
            var card = await CreateCardAsync();
            var res = await cardRepository.IsExistsAsync(card.Number);
            Assert.True(res);
        }
        #endregion

        #region IsActiveAsync
        [Fact]
        public async Task IsActiveAsync_WithNotExists()
        {
            var cn = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.IsActiveAsync(cn);
            Assert.False(res);
        }

        [Fact]
        public async Task IsActiveAsync_ForExistCard()
        {
            var card = await CreateCardAsync();
            var res = await cardRepository.IsActiveAsync(card.Number);
            Assert.True(res);
        }

        [Fact]
        public async Task IsActiveAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var res = await cardRepository.IsActiveAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task IsActiveAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var res = await cardRepository.IsActiveAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task IsActiveAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var res = await cardRepository.IsActiveAsync(card.Number);
            Assert.False(res);
        }
        #endregion

        #region OpenAsync
        [Fact]
        public async Task OpenAsync_WithNotExistsAccount()
        {
            var an = AccountNumberGenerator.Generate(408, 01, 613, DataGenerators.BIK);
            var card = new Card() {
                AccountNumber = an,
                Number = CardNumberGenerator.Generate(DataGenerators.BIN),
                ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(3)
            };
            card = await cardRepository.OpenAsync(card);
            Assert.Null(card);
        }

        [Fact]
        public async Task OpenAsync_WithInvalidCardNumber()
        {
            var cn = CardNumberGenerator.Generate(DataGenerators.BIN);
            cn = string.Concat("0", cn.AsSpan(1));
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            var card = DataGenerators.CreateRandomCard(account.Number);
            card.Number = cn;
            var pin = Random.Shared.Next(1000, 9000);
            var cvc = Random.Shared.Next(100, 900);
            card.PinHash = Hasher.Hash(pin.ToString());
            var cvcKeyId = await depedencyFixture.KeyStore.WriteAsync(cvc.ToString());
            card.CvcKeyId = cvcKeyId;
            card = await cardRepository.OpenAsync(card);
            Assert.Null(card);
        }

        [Fact]
        public async Task OpenAsync_WithInvalidCardNumberLength()
        {
            var cn = CardNumberGenerator.Generate(DataGenerators.BIN);
            cn = cn[..8];
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            var card = DataGenerators.CreateRandomCard(account.Number);
            card.Number = cn;
            var pin = Random.Shared.Next(1000, 9000);
            var cvc = Random.Shared.Next(100, 900);
            card.PinHash = Hasher.Hash(pin.ToString());
            var cvcKeyId = await depedencyFixture.KeyStore.WriteAsync(cvc.ToString());
            card.CvcKeyId = cvcKeyId;
            card = await cardRepository.OpenAsync(card);
            Assert.Null(card);
        }

        [Fact]
        public async Task OpenAsync_WithValidData()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            var card = DataGenerators.CreateRandomCard(account.Number);
            var pin = Random.Shared.Next(1000, 9000);
            var cvc = Random.Shared.Next(100, 900);
            card.PinHash = Hasher.Hash(pin.ToString());
            var cvcKeyId = await depedencyFixture.KeyStore.WriteAsync(cvc.ToString());
            card.CvcKeyId = cvcKeyId;
            card = await cardRepository.OpenAsync(card);
            Assert.NotNull(card);
        }
        #endregion

        #region GetAccountNumberAsync
        [Fact]
        public async Task GetAccountNumberAsync_ForNotExistCard()
        {
            var an = await cardRepository.GetAccountNumberAsync(CardNumberGenerator.Generate(DataGenerators.BIN));
            Assert.Null(an);
        }

        [Fact]
        public async Task GetAccountNumberAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var an = await cardRepository.GetAccountNumberAsync(card.Number);
            Assert.Null(an);
        }

        [Fact]
        public async Task GetAccountNumberAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var an = await cardRepository.GetAccountNumberAsync(card.Number);
            Assert.Null(an);
        }

        [Fact]
        public async Task GetAccountNumberAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var an = await cardRepository.GetAccountNumberAsync(card.Number);
            Assert.Null(an);
        }

        [Fact]
        public async Task GetAccountNumberAsync_ForValidCard()
        {
            var card = await CreateCardAsync();
            var an = await cardRepository.GetAccountNumberAsync(card.Number);
            Assert.NotNull(an);
        }
        #endregion

        #region GetPinHashAsync
        [Fact]
        public async Task GetPinHashAsync_ForNotExistCard()
        {
            var an = await cardRepository.GetPinHashAsync(CardNumberGenerator.Generate(DataGenerators.BIN));
            Assert.Null(an);
        }

        [Fact]
        public async Task GetPinHashAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var an = await cardRepository.GetPinHashAsync(card.Number);
            Assert.Null(an);
        }

        [Fact]
        public async Task GetPinHashAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var an = await cardRepository.GetPinHashAsync(card.Number);
            Assert.Null(an);
        }

        [Fact]
        public async Task GetPinHashAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var an = await cardRepository.GetPinHashAsync(card.Number);
            Assert.Null(an);
        }

        [Fact]
        public async Task GetPinHashAsync_ForValidCard()
        {
            var card = await CreateCardAsync();
            var an = await cardRepository.GetPinHashAsync(card.Number);
            Assert.NotNull(an);
        }
        #endregion

        #region ChangePinHashAsync
        [Fact]
        public async Task ChangePinHashAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var newPinHash = Hasher.Hash("312");
            var res = await cardRepository.ChangePinHashAsync(card.Number, newPinHash);
            Assert.False(res);
        }

        [Fact]
        public async Task ChangePinHashAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var newPinHash = Hasher.Hash("312");
            var res = await cardRepository.ChangePinHashAsync(card.Number, newPinHash);
            Assert.False(res);
        }

        [Fact]
        public async Task ChangePinHashAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var newPinHash = Hasher.Hash("312");
            var res = await cardRepository.ChangePinHashAsync(card.Number, newPinHash);
            Assert.False(res);
        }

        [Fact]
        public async Task ChangePinHashAsync_ForNotExistCard()
        {
            var cardNumber = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.ChangePinHashAsync(cardNumber,Hasher.Hash("312"));
            Assert.False(res);
        }

        [Fact]
        public async Task ChangePinHashAsync_ForValidCard()
        {
            var card = await CreateCardAsync();
            var newPinHash = Hasher.Hash("312");
            var res = await cardRepository.ChangePinHashAsync(card.Number, newPinHash);
            Assert.True(res);
        }
        #endregion

        #region GetCvcKeyId
        [Fact]
        public async Task GetCvcKeyIdAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var res = await cardRepository.GetCvcKeyIdAsync(card.Number);
            Assert.False(res.HasValue);
        }

        [Fact]
        public async Task GetCvcKeyIdAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var res = await cardRepository.GetCvcKeyIdAsync(card.Number);
            Assert.False(res.HasValue);
        }

        [Fact]
        public async Task GetCvcKeyIdAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var res = await cardRepository.GetCvcKeyIdAsync(card.Number);
            Assert.False(res.HasValue);
        }

        [Fact]
        public async Task GetCvcKeyIdAsync_ForNotExistCard()
        {
            var cardNumber = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.GetCvcKeyIdAsync(cardNumber);
            Assert.False(res.HasValue);
        }

        [Fact]
        public async Task GetCvcKeyIdAsync_ForValidCard()
        {
            var card = await CreateCardAsync();
            var res = await cardRepository.GetCvcKeyIdAsync(card.Number);
            Assert.True(res.HasValue);
        }
        #endregion

        #region GetAllByAccountNumber
        [Fact]
        public async Task GetAllByAccountNumber_WithExistsAccount()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var account = DataGenerators.CreateRandomAccount(client.Id);
            account = await accountRepository.OpenAsync(account);
            Assert.NotNull(account);
            for (int i = 0; i < 4; i++)
            {
                await CreateCardAsync(account.Number);
            }

            var cards = await cardRepository.GetAllByAccountNumberAsync(account.Number);
            Assert.NotEmpty(cards);
        }

        [Fact]
        public async Task GetAllByAccountNumber_WithNotExistsAccount()
        {
            var accountNumber = AccountNumberGenerator.Generate(408, 01, 613, DataGenerators.BIK);
            var cards = await cardRepository.GetAllByAccountNumberAsync(accountNumber);
            Assert.Empty(cards);
        }
        #endregion

        #region FreezeAsync
        [Fact]
        public async Task FreezeAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var res = await cardRepository.FreezeAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task FreezeAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var res = await cardRepository.FreezeAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task FreezeAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var res = await cardRepository.FreezeAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task FreezeAsync_ForNotExistCard()
        {
            var cardNumber = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.FreezeAsync(cardNumber);
            Assert.False(res);
        }
        #endregion

        #region UnfreezeAsync
        [Fact]
        public async Task UnfreezeAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var res = await cardRepository.UnfreezeAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task UnfreezeAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var res = await cardRepository.UnfreezeAsync(card.Number);
            Assert.True(res);
        }

        [Fact]
        public async Task UnfreezeAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var res = await cardRepository.UnfreezeAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task UnfreezeAsync_ForNotExistCard()
        {
            var cardNumber = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.UnfreezeAsync(cardNumber);
            Assert.False(res);
        }
        #endregion

        #region LockAsync
        [Fact]
        public async Task LockAsync_ForExpiriedCard()
        {
            var card = await CreateExpiriedCardAsync();
            var res = await cardRepository.LockAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task LockAsync_ForFrozedCard()
        {
            var card = await CreateFrozedCardAsync();
            var res = await cardRepository.LockAsync(card.Number);
            Assert.True(res);
        }

        [Fact]
        public async Task LockAsync_ForLockedCard()
        {
            var card = await CreateLockedCardAsync();
            var res = await cardRepository.LockAsync(card.Number);
            Assert.False(res);
        }

        [Fact]
        public async Task LockAsync_ForNotExistCard()
        {
            var cardNumber = CardNumberGenerator.Generate(DataGenerators.BIN);
            var res = await cardRepository.LockAsync(cardNumber);
            Assert.False(res);
        }
        #endregion

        #region Utils
        private async Task<Card> CreateCardAsync(string? accountNumber = null, bool expiried = false, bool locked = false, bool frozed = false)
        {
            if (accountNumber == null)
            {
                var client = DataGenerators.CreateRandomClient();
                client = await clientRepository.AddAsync(client);
                Assert.NotNull(client);
                var account = DataGenerators.CreateRandomAccount(client.Id);
                account = await accountRepository.OpenAsync(account);
                Assert.NotNull(account);
                accountNumber = account.Number;
            }
            var card = DataGenerators.CreateRandomCard(accountNumber);
            if(expiried)
                card.ValidUntil = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3);
            var pin = Random.Shared.Next(1000, 9000);
            var cvc = Random.Shared.Next(100, 900);
            card.PinHash = Hasher.Hash(pin.ToString());
            var cvcKeyId = await depedencyFixture.KeyStore.WriteAsync(cvc.ToString());
            card.CvcKeyId = cvcKeyId;
            card = await cardRepository.OpenAsync(card);
            Assert.NotNull(card);
            if(locked)
                await cardRepository.LockAsync(card.Number);
            if(frozed)
                await cardRepository.FreezeAsync(card.Number);
            return card;
        }

        private async Task<Card> CreateExpiriedCardAsync(string? accountNumber = null) => await CreateCardAsync(accountNumber, expiried: true);


        private async Task<Card> CreateLockedCardAsync(string? accountNumber = null) => await CreateCardAsync(accountNumber, locked: true);

        private async Task<Card> CreateFrozedCardAsync(string? accountNumber = null) => await CreateCardAsync(accountNumber, frozed: true);
        #endregion
    }
}
