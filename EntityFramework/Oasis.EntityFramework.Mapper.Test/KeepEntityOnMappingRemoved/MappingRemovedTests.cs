﻿namespace Oasis.EntityFramework.Mapper.Test.KeepEntityOnMappingRemoved;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity;

public sealed class MappingRemovedTests : TestBase
{
    [Test]
    public void PropertyConfig_WrongPropertyName_ShouldThrowException()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, false)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(false)
                .Finish()
            .Configure<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
                .SetMappingKeepEntityOnMappingRemoved(false)
                .PropertyKeepEntityOnMappingRemoved("NoneExistingPropertyName", true)
                .Finish()
            .Register<MappingRemovedPrincipal1, MappingRemovedPrincipal2>();

        // act and assert
        Assert.Throws<CustomTypePropertyEntityRemoverException>(() => mapper.Build());
    }

    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task DefaultConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, defaultKeep).RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task DefaultConfig_RemoveListItm_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, defaultKeep).RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task TypeConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, !defaultKeep)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(defaultKeep)
                .Finish()
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
            .Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task TypeConfig_RemoveListItem_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, !defaultKeep)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(defaultKeep)
                .Finish()
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
            .Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task MappingConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, !defaultKeep)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(!defaultKeep)
                .Finish()
            .Configure<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
                .SetMappingKeepEntityOnMappingRemoved(defaultKeep)
                .Finish()
            .Register<MappingRemovedPrincipal1, MappingRemovedPrincipal2>()
            .Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task MappingConfig_RemoveListItem_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, !defaultKeep)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(!defaultKeep)
                .Finish()
            .Configure<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
                .SetMappingKeepEntityOnMappingRemoved(defaultKeep)
                .Finish()
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task PropertyConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, !defaultKeep)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(!defaultKeep)
                .Finish()
            .Configure<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
                .SetMappingKeepEntityOnMappingRemoved(!defaultKeep)
                .PropertyKeepEntityOnMappingRemoved(nameof(MappingRemovedPrincipal1.OptionalDependant), defaultKeep)
                .Finish()
            .Register<MappingRemovedPrincipal1, MappingRemovedPrincipal2>().Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public async Task PropertyConfig_RemoveListItem_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder(null, !defaultKeep)
            .Configure<MappingRemovedDependant1>()
                .SetKeepEntityOnMappingRemoved(!defaultKeep)
                .Finish()
            .Configure<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
                .SetMappingKeepEntityOnMappingRemoved(!defaultKeep)
                .PropertyKeepEntityOnMappingRemoved(nameof(MappingRemovedPrincipal1.DependantList), defaultKeep)
                .Finish()
            .Register<MappingRemovedPrincipal1, MappingRemovedPrincipal2>().Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    public async Task RemoveEntityTest(IMapper mapper, int dependantCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new MappingRemovedPrincipal2 { OptionalDependant = new MappingRemovedDependant2 { IntProp = 1 } };
            var mappedPrincipal = await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.AreEqual(1, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });

        MappingRemovedPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<MappingRemovedPrincipal1>().Include(p => p.OptionalDependant).FirstAsync();
            mappedPrincipal = mapper.Map<MappingRemovedPrincipal1, MappingRemovedPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.OptionalDependant = null;
            await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(mappedPrincipal, p => p.Include(p => p.OptionalDependant), databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.AreEqual(dependantCount, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });
    }

    public async Task RemoveListItemTest(IMapper mapper, int dependantCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new MappingRemovedPrincipal2 { DependantList = new List<MappingRemovedDependant2> { new MappingRemovedDependant2 { IntProp = 1 } } };
            var mappedPrincipal = await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.AreEqual(1, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });

        MappingRemovedPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<MappingRemovedPrincipal1>().Include(p => p.DependantList).FirstAsync();
            mappedPrincipal = mapper.Map<MappingRemovedPrincipal1, MappingRemovedPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.DependantList.Clear();
            await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(mappedPrincipal, p => p.Include(p => p.DependantList), databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.AreEqual(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.AreEqual(dependantCount, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });
    }
}
