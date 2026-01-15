using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_ins.Repositories;
using test_ins.Models;

namespace test_ins.Tests
{
    [TestClass]
    public class RateTierCrudTests
    {
        [TestMethod]
        public void CreateUpdateDeleteTier_InMemoryRepo()
        {
            var repo = new InMemoryRepo();
            var admin = new User { Email = "admin@t.local", IsAdmin = true };
            repo.AddUser(admin);

            var tier = new RateTierEntity { Name = "gold", RequestsPerMinute = 500, Description = "Gold tier" };
            var created = repo.CreateRateTier(tier);
            Assert.IsNotNull(repo.GetRateTier(created.Id));

            created.RequestsPerMinute = 600;
            repo.UpdateRateTier(created);
            var fetched = repo.GetRateTier(created.Id);
            Assert.AreEqual(600, fetched!.RequestsPerMinute);

            repo.DeleteRateTier(created.Id);
            Assert.IsNull(repo.GetRateTier(created.Id));
        }
    }
}
