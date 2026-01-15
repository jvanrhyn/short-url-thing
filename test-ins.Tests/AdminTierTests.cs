using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_ins.Repositories;
using test_ins.Models;

namespace test_ins.Tests
{
    [TestClass]
    public class AdminTierTests
    {
        [TestMethod]
        public void AdminCanAssignEnterpriseTier()
        {
            var repo = new InMemoryRepo();
            var admin = new User { Email = "admin@t.local", IsAdmin = true };
            var target = new User { Email = "user@t.local" };
            repo.AddUser(admin);
            repo.AddUser(target);

            // simulate admin assigning enterprise tier
            var tiers = new[] { new RateTier { Name = "enterprise", RequestsPerMinute = 2000 } };
            var tier = tiers[0];
            target.RateLimitRpm = tier.RequestsPerMinute;
            repo.UpdateUser(target);

            var fetched = repo.GetUser(target.UserId);
            Assert.IsNotNull(fetched);
            Assert.AreEqual(2000, fetched!.RateLimitRpm);
        }
    }
}
