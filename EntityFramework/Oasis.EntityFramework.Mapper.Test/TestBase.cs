﻿namespace Oasis.EntityFramework.Mapper.Test;

using NUnit.Framework;
using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

public abstract class TestBase
{
    private DbConnection? _connection;

    [SetUp]
    public void Setup()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        var sql = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}/script.sql");
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    protected static IMapperBuilder MakeDefaultMapperBuilder(IMapperBuilderFactory factory)
    {
        return factory.MakeMapperBuilder(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken));
    }

    protected static IMapperBuilder MakeDefaultMapperBuilder(IMapperBuilderFactory factory, bool keepEntityOnMappingRemoved)
    {
        return factory.MakeMapperBuilder(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), null, keepEntityOnMappingRemoved);
    }

    protected DbContext CreateDatabaseContext()
    {
        var databaseContext = new DatabaseContext(_connection!);

        return databaseContext;
    }

    protected async Task ExecuteWithNewDatabaseContext(Func<DbContext, Task> action)
    {
        using (var databaseContext = CreateDatabaseContext())
        {
            await action(databaseContext);
        }
    }
}
