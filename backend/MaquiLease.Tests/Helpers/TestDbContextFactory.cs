using MaquiLease.API.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace MaquiLease.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static AppDbContext Create()
        {
            var dbName = $"MaquiLease_Test_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
