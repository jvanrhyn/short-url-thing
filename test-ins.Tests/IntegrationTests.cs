using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace test_ins.Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private readonly WebApplicationFactory<Program> _factory = new();

        [TestMethod]
        public async Task CreateUser_CreateUrl_Redirects_RecordEvent_And_Stats()
        {
            var client = _factory.CreateClient();

            // create user
            var userPayload = new { email = "inttest@example.local" };
            var resp = await client.PostAsync("/users", new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var userJson = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var apiKey = userJson.GetProperty("apiKey").GetString();
            Assert.IsNotNull(apiKey);

            // create short url
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            var urlPayload = new { destination = "https://example.com" };
            resp = await client.PostAsync("/urls", new StringContent(JsonSerializer.Serialize(urlPayload), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var created = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var id = created.GetProperty("id").GetGuid();
            var shortCode = created.GetProperty("shortCode").GetString();

            // perform redirect
            resp = await client.GetAsync($"/r/{shortCode}");
            Assert.AreEqual(HttpStatusCode.Redirect, resp.StatusCode);

            // check stats
            resp = await client.GetAsync($"/urls/{id}/stats");
            resp.EnsureSuccessStatusCode();
            var stats = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            Assert.IsTrue(stats.GetProperty("redirects").GetInt32() >= 1);
            Assert.IsTrue(stats.GetProperty("dailyCounts").GetArrayLength() >= 1);
        }

        [TestMethod]
        public async Task AdminCreatesTinyTier_AssignsToUser_UserIsRateLimited()
        {
            var client = _factory.CreateClient();

            // seed admin from in-memory dev user (dev-api-key-123)
            client.DefaultRequestHeaders.Add("X-Api-Key", "dev-api-key-123");

            // create tiny tier (1 rpm)
            var tierPayload = new { name = "tiny", requestsPerMinute = 1, description = "test tiny" };
            var resp = await client.PostAsync("/admin/tiers", new StringContent(JsonSerializer.Serialize(tierPayload), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var createdTier = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var tierName = createdTier.GetProperty("name").GetString();

            // create test user
            var userPayload = new { email = "ratetest@example.local" };
            resp = await client.PostAsync("/users", new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            var userJson = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var apiKey = userJson.GetProperty("apiKey").GetString();
            var userId = userJson.GetProperty("userId").GetGuid();

            // admin assign tiny tier to user
            var assignPayload = new { tierName = tierName };
            resp = await client.PatchAsync($"/admin/users/{userId}/rate-tier", new StringContent(JsonSerializer.Serialize(assignPayload), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            // user should now be limited to 1 req/min on protected endpoints
            var clientUser = _factory.CreateClient();
            clientUser.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            // first request ok
            var r1 = await clientUser.GetAsync("/urls");
            Assert.AreEqual(HttpStatusCode.OK, r1.StatusCode);
            // second within same minute should be rate limited
            var r2 = await clientUser.GetAsync("/urls");
            Assert.AreEqual((HttpStatusCode)429, r2.StatusCode);
        }
    }
}
