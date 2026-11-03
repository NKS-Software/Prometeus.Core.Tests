using Prometeus.Core.Data.Repositories;
using Prometeus.Core.Tests.Utils;

namespace Prometeus.Core.Tests.Repositories
{
    public class ClientRepositoryTests(DepedencyFixture depedencyFixture) : IClassFixture<DepedencyFixture>
    {
        private readonly ClientRepository clientRepository = depedencyFixture.DatabaseContext.Get<ClientRepository>();
        #region IsExistsAsync
        [Fact]
        public async Task IsExistsAsync_WithInvalidGuid()
        {
            var guid = Guid.NewGuid();
            var r = await clientRepository.IsExistsAsync(guid);
            Assert.False(r);
        }

        [Fact]
        public async Task IsExistsAsync_WithValidGuid()
        {
            var client = DataGenerators.CreateRandomClient();
            var res = await clientRepository.AddAsync(client);
            Assert.NotNull(res);
            var r = await clientRepository.IsExistsAsync(res.Id);
            Assert.True(r);
        }
        #endregion

        #region EmailAlreadyUsedAsync
        [Fact]
        public async Task EmailAlreadyUsedAsync_WithNotUsedEmail()
        {
            var email = "testemail1234@gmail.com";
            var r = await clientRepository.EmailAlreadyUsedAsync(email);
            Assert.False(r);
        }

        [Fact]
        public async Task EmailAlreadyUsed_WithUsedEmail()
        {
            var client = DataGenerators.CreateRandomClient();
            var res = await clientRepository.AddAsync(client);
            Assert.NotNull(res);
            var r = await clientRepository.EmailAlreadyUsedAsync(client.Email);
            Assert.True(r);
        }
        #endregion

        #region PhoneAlreadyUsedAsync
        [Fact]
        public async Task PhoneAlreadyUsedAsync_WithNotUsedPhone()
        {
            var r = await clientRepository.PhoneAlreadyUsedAsync("+79990001122");
            Assert.False(r);
        }

        [Fact]
        public async Task PhoneAlreadyUsed_WithUsedPhoone()
        {
            var client = DataGenerators.CreateRandomClient();
            var res = await clientRepository.AddAsync(client);
            Assert.NotNull(res);
            var r = await clientRepository.PhoneAlreadyUsedAsync(client.Phone);
            Assert.True(r);
        }
        #endregion

        #region GetIdByEmailAsync
        [Fact]
        public async Task GetIdByEmailAsync_WithNotExistedClient()
        {
            var id = await clientRepository.GetIdByEmailAsync("gamer1234@gmail.com");
            Assert.Null(id);
        }

        [Fact]
        public async Task GetIdByEmailAsync_WithExistedClient()
        {
            
            var client = DataGenerators.CreateRandomClient();
            var res = await clientRepository.AddAsync(client);
            Assert.NotNull(res);
            var id = await clientRepository.GetIdByEmailAsync(client.Email);
            Assert.NotNull(id);
        }
        #endregion

        #region GetIdByPhoneAsync
        [Fact]
        public async Task GetIdByPhoneAsync_WithNotExistedClient()
        {
            var id = await clientRepository.GetIdByPhoneAsync("+79991572116");
            Assert.Null(id);
        }

        [Fact]
        public async Task GetIdByPhoneAsync_WithExistedClient()
        {
            var client = DataGenerators.CreateRandomClient();
            var res = await clientRepository.AddAsync(client);
            Assert.NotNull(res);
            var id = await clientRepository.GetIdByPhoneAsync(client.Phone);
            Assert.NotNull(id);
        }
        #endregion

        #region GetByIdAsync
        [Fact]
        public async Task GetByIdAsync_WithNotExist()
        {
            var id = Guid.CreateVersion7();
            var client = await clientRepository.GetByIdAsync(id);
            Assert.Null(client);
        }

        [Fact]
        public async Task GetByIdAsync_WithExist()
        {
            var client = DataGenerators.CreateRandomClient();
            client = await clientRepository.AddAsync(client);
            Assert.NotNull(client);
            var client2 = await clientRepository.GetByIdAsync(client.Id);
            Assert.NotNull(client2);
        }
        #endregion

        #region AddAsync
        [Fact]
        public async Task AddAsync_WithDuplicatedEmail()
        {
            var clientA = DataGenerators.CreateRandomClient();
            var clientB = DataGenerators.CreateRandomClient();
            clientA.Email = "qwer4321@gmail.com";
            clientB.Email = "qwer4321@gmail.com";
            await clientRepository.AddAsync(clientA);
            var newClientB = await clientRepository.AddAsync(clientB);
            Assert.Null(newClientB);
        }

        [Fact]
        public async Task AddAsync_WithDuplicatedPhone()
        {
            var clientA = DataGenerators.CreateRandomClient();
            var clientB = DataGenerators.CreateRandomClient();
            clientA.Phone = "+79061572115";
            clientB.Phone = "+79061572115";
            await clientRepository.AddAsync(clientA);
            var newClientB = await clientRepository.AddAsync(clientB);
            Assert.Null(newClientB);
        }

        [Fact]
        public async Task AddAsync_WithInvalidFormatEmail()
        {
            var client = DataGenerators.CreateRandomClient();
            client.Email = "qwer4321gmail.com";
            var newClient = await clientRepository.AddAsync(client);
            Assert.Null(newClient);
        }

        [Fact]
        public async Task AddAsync_WithInvalidFormatPhone()
        {
            var client = DataGenerators.CreateRandomClient();
            client.Phone = "89061572114";
            var newClient = await clientRepository.AddAsync(client);
            Assert.Null(newClient);
        }
        #endregion

        #region UpdateAsync
        [Fact]
        public async Task UpdateAsync_WithNotExistedClient()
        {
            var client = await clientRepository.UpdateAsync(Guid.NewGuid(), name: "Cool boy");
            Assert.Null(client);
        }

        [Fact]
        public async Task UpdateAsync_WithExistedClient()
        {
            var client = await clientRepository.AddAsync(DataGenerators.CreateRandomClient(name: "Nikita"));
            Assert.NotNull(client);
            var updatedClient = await clientRepository.UpdateAsync(client.Id, name: "Michail");
            Assert.NotNull(updatedClient);
            Assert.NotEqual(client.Name, updatedClient.Name);
        }
        #endregion

        #region ChangeEmailAsync
        [Fact]
        public async Task ChangeEmailAsync_WithNotExistedClient()
        {
            var updatedClient = await clientRepository.ChangeEmailAsync(Guid.NewGuid(), "hacker1234@gmail.com");
            Assert.Null(updatedClient);
        }

        [Fact]
        public async Task ChangeEmailAsync_WiithExistedClient()
        {
            
            var client = await clientRepository.AddAsync(DataGenerators.CreateRandomClient(emailPrefix: "email"));
            Assert.NotNull(client);
            var updatedClient = await clientRepository.ChangeEmailAsync(client.Id, "hacker1234@gmail.com");
            Assert.NotNull(updatedClient);
            Assert.NotEqual(client.Email, updatedClient.Email);
        }

        [Fact]
        public async Task ChangeEmailAsync_WithDuplicatedEmail()
        {
            var clientA = DataGenerators.CreateRandomClient();
            var clientB = DataGenerators.CreateRandomClient();
            clientA.Email = "qwer4321@gmail.com";
            await clientRepository.AddAsync(clientA);
            var newClientB = await clientRepository.AddAsync(clientB);
            Assert.NotNull(newClientB);
            var updatedClientB = await clientRepository.ChangeEmailAsync(newClientB.Id, "qwer4321@gmail.com");
            Assert.Null(updatedClientB);
        }
        #endregion

        #region ChangePhoneAsync
        [Fact]
        public async Task ChangePhoneAsync_WithNotExistedClient()
        {
            var updatedClient = await clientRepository.ChangePhoneAsync(Guid.NewGuid(), "+79990001133");
            Assert.Null(updatedClient);
        }

        [Fact]
        public async Task ChangePhoneAsync_WithExistedClient()
        {
            var client = await clientRepository.AddAsync(DataGenerators.CreateRandomClient());
            Assert.NotNull(client);
            var updatedClient = await clientRepository.ChangePhoneAsync(client.Id, "+79990001133");
            Assert.NotNull(updatedClient);
            Assert.NotEqual(client.Phone, updatedClient.Phone);
        }

        [Fact]
        public async Task ChangePhoneAsync_WithDuplicatedPhone()
        {
            var clientA = DataGenerators.CreateRandomClient();
            var clientB = DataGenerators.CreateRandomClient();
            clientA.Phone = "+79081562115";
            await clientRepository.AddAsync(clientA);
            var newClientB = await clientRepository.AddAsync(clientB);
            Assert.NotNull(newClientB);
            var updatedClientB = await clientRepository.ChangePhoneAsync(newClientB.Id, "+79081562115");
            Assert.Null(updatedClientB);
        }
        #endregion

        #region DeleteAsync
        [Fact]
        public async Task DeleteAsync_WithNotExistedClient()
        {
            var r = await clientRepository.DeleteAsync(Guid.NewGuid());
            Assert.False(r);
        }

        [Fact]
        public async Task DeleteAsync_WithExistedClient()
        {
            
            var client = await clientRepository.AddAsync(DataGenerators.CreateRandomClient());
            Assert.NotNull(client);
            var r = await clientRepository.DeleteAsync(client.Id);
            Assert.True(r);
        }
        #endregion
    }
}
