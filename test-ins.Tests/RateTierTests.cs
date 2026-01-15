#if false // disabled; enable once test SDK supports net10 in CI
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_ins.Repositories;
using test_ins.Services;
using test_ins.Models;
using System.Linq;

namespace test_ins.Tests
{
    [TestClass]
    public class RateTierTests
    {
        [TestMethod]
        public void UserCanUpdateRateTier_InMemoryRepo()
        {
            var repo = new InMemoryRepo();
            var user = new User { Email = "t@t.local" };
            repo.AddUser(user);

            // simulate patching user's tier
            user.RateLimitRpm = 300;
            repo.UpdateUser(user);

            var fetched = repo.GetUser(user.UserId);
            Assert.IsNotNull(fetched);
            Assert.AreEqual(300, fetched!.RateLimitRpm);
        }
    }
}
#endif
