using System;
using test_ins.Models;
using test_ins.Repositories;
using test_ins.Services;
using test_ins.DTOs;
using Xunit;

namespace test_ins.Tests
{
    public class UrlServiceTests
    {
        [Fact]
        public void CreateAndRedirect_IncrementsCount()
        {
            var repo = new InMemoryRepo();
            var svc = new UrlService(repo);
            var user = new User { Email = "a@b.c", ApiKey = "k" };
            repo.AddUser(user);

            var req = new ShortUrlCreate { Destination = "https://example.com" };
            var s = svc.Create(user, req);
            Assert.NotNull(s);
            Assert.Equal(0, s.RedirectCount);

            svc.IncrementRedirect(s);
            var stored = svc.GetById(s.Id);
            Assert.Equal(1, stored!.RedirectCount);
        }
    }
}
