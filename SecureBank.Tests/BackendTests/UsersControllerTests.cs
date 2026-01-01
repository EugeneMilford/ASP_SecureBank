using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecureBank.API.Controllers;
using SecureBank.API.Data;
using SecureBank.API.Models.Domain;
using SecureBank.API.Models.DTO;
using Xunit;

namespace SecureBank.Tests.BackendTests
{
    // UsersController tests that run against a freshly-created SQL Server database per test.
    // No appsettings.json is embedded here — the JWT secret is provided via in-memory configuration.
    // Note: ensure SQL Server / LocalDB is available where tests run.
    public class UsersControllerTests : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseConnectionString;

        public UsersControllerTests()
        {
            // Minimal configuration with only the JWT secret
            var secret = Environment.GetEnvironmentVariable("API_TEST_SECRET")
                         ?? "TestSecretKey_For_UsersController_Tests_DoNotUseInProd_123456";

            var inMemorySettings = new Dictionary<string, string>
            {
                ["ApiSettings:Secret"] = secret
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Connection string to use as the base. Prefer TEST_SQL_CONNECTION env var, otherwise default to LocalDB.
            _baseConnectionString = Environment.GetEnvironmentVariable("TEST_SQL_CONNECTION")
                ?? "Server=(localdb)\\mssqllocaldb;Integrated Security=true;TrustServerCertificate=True;";
        }

        public void Dispose()
        {
            // cleanup handled per-test by dropping databases; nothing global to dispose here
        }

        // Creates a new DbContextOptions using a newly created database name (unique).
        // Caller must Dispose contexts. The created database name is dropped at the end of the test via DropDatabase.
        private DbContextOptions<BankingContext> CreateContextOptionsWithNewDatabase(out string databaseName)
        {
            databaseName = $"SecureBankTests_{Guid.NewGuid():N}";

            // Build a connection string that uses the new database name
            var builder = new SqlConnectionStringBuilder(_baseConnectionString)
            {
                InitialCatalog = databaseName,
                MultipleActiveResultSets = true
            };

            // Create the database by connecting to master and running CREATE DATABASE
            var masterBuilder = new SqlConnectionStringBuilder(_baseConnectionString) { InitialCatalog = "master" };
            using (var conn = new SqlConnection(masterBuilder.ConnectionString))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE [{databaseName}]";
                cmd.ExecuteNonQuery();
            }

            var options = new DbContextOptionsBuilder<BankingContext>()
                .UseSqlServer(builder.ConnectionString)
                .Options;

            // Ensure EF creates the schema (tables, seed data if any)
            using (var ctx = new BankingContext(options))
            {
                ctx.Database.EnsureCreated();
            }

            return options;
        }

        private void DropDatabase(string databaseName)
        {
            try
            {
                var masterBuilder = new SqlConnectionStringBuilder(_baseConnectionString) { InitialCatalog = "master" };
                using var conn = new SqlConnection(masterBuilder.ConnectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // best-effort cleanup; swallow exceptions to avoid hiding test failures
            }
        }

        [Fact]
        public async Task Login_Success_ReturnsTokenAndUserDetails()
        {
            var options = CreateContextOptionsWithNewDatabase(out var dbName);
            try
            {
                using var ctx = new BankingContext(options);

                var user = new User
                {
                    FirstName = "Jane",
                    LastName = "Doe",
                    Email = "jane@example.com",
                    Username = "jane",
                    Password = "Password123!",
                    PhoneNumber = "+1000000000",
                    Role = "User",
                    CreatedDate = DateTime.UtcNow
                };

                ctx.users.Add(user);
                await ctx.SaveChangesAsync();

                var controller = new UsersController(ctx, _configuration);

                var request = new UserLoginRequestDto
                {
                    Username = "jane",
                    Password = "Password123!"
                };

                var actionResult = await controller.Login(request);

                var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
                var response = Assert.IsType<UserLoginResponseDto>(ok.Value);
                Assert.False(string.IsNullOrWhiteSpace(response.Token));
                Assert.NotNull(response.UserDetails);
                Assert.Equal("jane", response.UserDetails.Username);
                Assert.Equal(3, response.Token.Split('.').Length);
            }
            finally
            {
                DropDatabase(dbName);
            }
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var options = CreateContextOptionsWithNewDatabase(out var dbName);
            try
            {
                using var ctx = new BankingContext(options);

                // Seed complete user record (avoid null constraint failures)
                ctx.users.Add(new User
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john@example.com",
                    Username = "john",
                    Password = "Secret1!",
                    PhoneNumber = "+1000000001",
                    Role = "User",
                    CreatedDate = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();

                var controller = new UsersController(ctx, _configuration);

                var badRequest = new UserLoginRequestDto
                {
                    Username = "john",
                    Password = "WrongPassword"
                };

                var result = await controller.Login(badRequest);

                var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
                Assert.Equal("Invalid username or password.", unauthorized.Value);
            }
            finally
            {
                DropDatabase(dbName);
            }
        }

        [Fact]
        public async Task Register_Success_CreatesUserAndReturnsResponse()
        {
            var options = CreateContextOptionsWithNewDatabase(out var dbName);
            try
            {
                using var ctx = new BankingContext(options);

                var controller = new UsersController(ctx, _configuration);

                var request = new UserRegisterRequestDto
                {
                    FirstName = "Alice",
                    LastName = "Smith",
                    Email = "alice@example.com",
                    Username = "alice",
                    Password = "AlicePass1!",
                    PhoneNumber = "+1999999999",
                    Role = "User"
                };

                var actionResult = await controller.Register(request);

                var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
                var response = Assert.IsType<UserRegisterResponseDto>(ok.Value);
                Assert.True(response.UserId > 0);
                Assert.Equal("alice", response.Username);
                Assert.Equal("User", response.Role);
                Assert.Equal("Registration successful.", response.Message);

                var saved = await ctx.users.FirstOrDefaultAsync(u => u.Username == "alice");
                Assert.NotNull(saved);
                Assert.Equal("alice@example.com", saved.Email);
            }
            finally
            {
                DropDatabase(dbName);
            }
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsBadRequest()
        {
            var options = CreateContextOptionsWithNewDatabase(out var dbName);
            try
            {
                using var ctx = new BankingContext(options);

                // Seed complete user record 
                ctx.users.Add(new User
                {
                    FirstName = "Bob",
                    LastName = "Builder",
                    Email = "bob@ex.com",
                    Username = "bob",
                    Password = "x",
                    PhoneNumber = "+1000000002",
                    Role = "User",
                    CreatedDate = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();

                var controller = new UsersController(ctx, _configuration);

                var request = new UserRegisterRequestDto
                {
                    Username = "bob",
                    Email = "bob2@ex.com",
                    Password = "something"
                };

                var result = await controller.Register(request);

                var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
                Assert.Equal("Username already exists.", bad.Value);
            }
            finally
            {
                DropDatabase(dbName);
            }
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            var options = CreateContextOptionsWithNewDatabase(out var dbName);
            try
            {
                using var ctx = new BankingContext(options);

                // Seed complete user record 
                ctx.users.Add(new User
                {
                    FirstName = "Charlie",
                    LastName = "Chaplin",
                    Email = "charlie@ex.com",
                    Username = "charlie",
                    Password = "x",
                    PhoneNumber = "+1000000003",
                    Role = "User",
                    CreatedDate = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();

                var controller = new UsersController(ctx, _configuration);

                var request = new UserRegisterRequestDto
                {
                    Username = "charlie2",
                    Email = "charlie@ex.com",
                    Password = "something"
                };

                var result = await controller.Register(request);

                var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
                Assert.Equal("Email already exists.", bad.Value);
            }
            finally
            {
                DropDatabase(dbName);
            }
        }
    }
}