#if false // currently disabled due to test SDK compatibility for net10; keep for quick enable later
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_ins.Repositories;
using test_ins.Services;
using test_ins.Models;

namespace test_ins.Tests
{
    [TestClass]
    public class AuditEventsTests
    {
        [TestMethod]
        public void CreateShortUrl_RecordsAudit_InMemoryRepo()
        {
            var repo = new InMemoryRepo();
            var svc = new UrlService(repo, new Microsoft.Extensions.Logging.Abstractions.NullLogger<UrlService>());
            var user = new User { Email = "audit@t.local" };
            repo.AddUser(user);

            var s = svc.Create(user, new DTOs.ShortUrlCreate { Destination = "https://example.com" });
            var audits = repo.ListAuditEvents("ShortUrl", s.Id, DateTimeOffset.UtcNow.AddMinutes(-5));
            Assert.AreEqual(1, System.Linq.Enumerable.Count(audits));
            var a = System.Linq.Enumerable.First(audits);
            Assert.AreEqual("shorturl:create", a.Action);
            Assert.AreEqual(user.UserId, a.ActorUserId);
        }
    }
}
#endif
