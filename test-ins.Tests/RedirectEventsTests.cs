using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using test_ins.Repositories;
using test_ins.Services;
using test_ins.Models;

namespace test_ins.Tests
{
    [TestClass]
    public class RedirectEventsTests
    {
        [TestMethod]
        public void UrlService_IncrementRecordsEvent_InMemoryRepo()
        {
            var repo = new InMemoryRepo();
            var svc = new UrlService(repo, new Microsoft.Extensions.Logging.Abstractions.NullLogger<UrlService>());
            var user = new User { Email = "t@t.local" };
            repo.AddUser(user);

            var s = svc.Create(user, new DTOs.ShortUrlCreate { Destination = "https://example.com" });
            Assert.AreEqual(0, s.RedirectCount);

            svc.IncrementRedirect(s);

            var fetched = svc.GetById(s.Id);
            Assert.IsNotNull(fetched);
            Assert.AreEqual(1, fetched!.RedirectCount);

            var events = repo.ListRedirectEvents(s.Id, DateTimeOffset.UtcNow.AddMinutes(-5));
            Assert.AreEqual(1, System.Linq.Enumerable.Count(events));
        }
    }
}
