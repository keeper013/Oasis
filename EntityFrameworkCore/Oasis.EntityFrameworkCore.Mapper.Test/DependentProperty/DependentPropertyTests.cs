﻿namespace Oasis.EntityFrameworkCore.Mapper.Test.DependentProperty;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public sealed class DependentPropertyTests : TestBase
{
    [Fact]
    public void PropertyConfig_WrongPropertyName_ShouldThrowException()
    {
        // arrange, act and assert
        Assert.Throws<InvalidDependentException>(() =>
        {
            MakeDefaultMapperBuilder(null)
            .Configure<DependentPropertyDependent1>()
                .SetDependentProperties("NoneExistingPropertyName")
                .Finish();
        });
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task RemoveEntity_Test(bool keep, int dependentCount)
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        if (!keep)
        {
            mapperBuilder = mapperBuilder.Configure<DependentPropertyPrincipal1>()
                .SetDependentProperties(nameof(DependentPropertyPrincipal1.OptionalDependent))
                .Finish();
        }

        var mapper = mapperBuilder.RegisterTwoWay<DependentPropertyPrincipal2, DependentPropertyPrincipal1>().Build();

        // act and assert
        await RemoveEntityTest(mapper, dependentCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task RemoveListItem_Test(bool keep, int dependentCount)
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        if (!keep)
        {
            mapperBuilder = mapperBuilder.Configure<DependentPropertyPrincipal1>()
                .SetDependentProperties(nameof(DependentPropertyPrincipal1.DependentList))
                .Finish();
        }

        var mapper = mapperBuilder.RegisterTwoWay<DependentPropertyPrincipal2, DependentPropertyPrincipal1>().Build();

        // act and assert
        await RemoveListItemTest(mapper, dependentCount);
    }

    public async Task RemoveEntityTest(IMapper mapper, int dependentCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new DependentPropertyPrincipal2 { OptionalDependent = new DependentPropertyDependent2 { IntProp = 1 } };
            var mappedPrincipal = await mapper.MapAsync<DependentPropertyPrincipal2, DependentPropertyPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<DependentPropertyPrincipal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<DependentPropertyDependent1>().CountAsync());
        });

        DependentPropertyPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<DependentPropertyPrincipal1>().Include(p => p.OptionalDependent).FirstAsync();
            mappedPrincipal = mapper.Map<DependentPropertyPrincipal1, DependentPropertyPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.OptionalDependent = null;
            await mapper.MapAsync<DependentPropertyPrincipal2, DependentPropertyPrincipal1>(mappedPrincipal, p => p.Include(p => p.OptionalDependent), databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<DependentPropertyPrincipal1>().CountAsync());
            Assert.Equal(dependentCount, await databaseContext.Set<DependentPropertyDependent1>().CountAsync());
        });
    }

    public async Task RemoveListItemTest(IMapper mapper, int dependentCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new DependentPropertyPrincipal2 { DependentList = new List<DependentPropertyDependent2> { new DependentPropertyDependent2 { IntProp = 1 } } };
            var mappedPrincipal = await mapper.MapAsync<DependentPropertyPrincipal2, DependentPropertyPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<DependentPropertyPrincipal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<DependentPropertyDependent1>().CountAsync());
        });

        DependentPropertyPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<DependentPropertyPrincipal1>().Include(p => p.DependentList).FirstAsync();
            mappedPrincipal = mapper.Map<DependentPropertyPrincipal1, DependentPropertyPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.DependentList.Clear();
            await mapper.MapAsync<DependentPropertyPrincipal2, DependentPropertyPrincipal1>(mappedPrincipal, p => p.Include(p => p.DependentList), databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<DependentPropertyPrincipal1>().CountAsync());
            Assert.Equal(dependentCount, await databaseContext.Set<DependentPropertyDependent1>().CountAsync());
        });
    }
}
