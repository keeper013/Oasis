﻿namespace LibrarySample;

using Google.Protobuf;
using Oasis.EntityFrameworkCore.Mapper.Sample;
using Oasis.EntityFrameworkCore.Mapper;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public sealed class TestCase3_MapNavigationProperties_WithUnmapped : TestBase
{
    [Fact]
    public async Task Test1_AddAndUpateBorrower()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, BorrowerAddress, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Fact]
    public async Task Test2_AddAndUpateBorrower_WithGlobalUnmappedProperty()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, new string[] { nameof(Contact.Address) });
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber), null)
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, null);
    }

    [Fact]
    public async Task Test3_AddAndUpateBorrower_WithTypeUnmappedProperty_NewContactDTO()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber), null)
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .WithConfiguration<NewContactDTO>(null, null, new[] { nameof(UpdateContactDTO.Address) })
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Fact]
    public async Task Test4_AddAndUpateBorrower_WithTypeUnmappedProperty_UpdateContactDTO()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .WithConfiguration<UpdateContactDTO>(null, null, new[] { nameof(UpdateContactDTO.Address) })
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, null, UpdatedBorrowerAddress, BorrowerAddress);
    }

    [Fact]
    public async Task Test5_AddAndUpateBorrower_WithTypeUnmappedProperty_Contact()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .WithConfiguration<Contact>(null, null, new[] { nameof(Contact.Address) })
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, null);
    }

    [Fact]
    public async Task Test6_AddAndUpateBorrower_WithTypeUnmappedProperty_NewDtoToEntity()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var newToContact = factory.MakeCustomTypeMapperBuilder<NewContactDTO, Contact>().ExcludePropertyByName(nameof(Contact.Address)).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .Register<NewContactDTO, Contact>(newToContact)
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, null, null, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Fact]
    public async Task Test7_AddAndUpateBorrower_WithTypeUnmappedProperty_EntityToUpdateDto()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var contactToUpdate = factory.MakeCustomTypeMapperBuilder<Contact, UpdateContactDTO>().ExcludePropertyByName(nameof(Contact.Address)).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .Register<Contact, UpdateContactDTO>(contactToUpdate)
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, null, UpdatedBorrowerAddress, UpdatedBorrowerAddress);
    }

    [Fact]
    public async Task Test8_AddAndUpateBorrower_WithTypeUnmappedProperty_UpdateDtoToEntity()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var updateToContact = factory.MakeCustomTypeMapperBuilder<UpdateContactDTO, Contact>().ExcludePropertyByName(nameof(Contact.Address)).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithScalarConverter<ByteString, byte[]>(bs => bs.ToByteArray())
            .WithConfiguration<Borrower>(nameof(Borrower.IdentityNumber), nameof(Borrower.ConcurrencyToken))
            .WithConfiguration<NewBorrowerDTO>(nameof(NewBorrowerDTO.IdentityNumber))
            .WithConfiguration<UpdateBorrowerDTO>(nameof(UpdateBorrowerDTO.IdentityNumber), nameof(UpdateBorrowerDTO.ConcurrencyToken))
            .Register<UpdateContactDTO, Contact>(updateToContact)
            .Register<NewBorrowerDTO, Borrower>()
            .RegisterTwoWay<Borrower, UpdateBorrowerDTO>()
            .Build();

        const string BorrowerAddress = "Dummy Address";
        const string UpdatedBorrowerAddress = "Updated Address 1";
        await AddAndUpateBorrower(mapper, BorrowerAddress, BorrowerAddress, BorrowerAddress, UpdatedBorrowerAddress, BorrowerAddress);
    }

    [Fact]
    public async Task Test9_AddBookWithCopies()
    {
        // initialize mapper
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .WithScalarConverter<byte[], ByteString>(arr => ByteString.CopyFrom(arr))
            .WithConfiguration<NewCopyDTO>(nameof(NewCopyDTO.Number))
            .WithConfiguration<Copy>(nameof(Copy.Number))
            .Register<NewBookDTO, Book>()
            .Register<Book, BookDTO>()
            .Build();

        // create new book
        const string BookName = "Book 1";
        Book book = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var bookDto = new NewBookDTO { Name = BookName };
            for (var i = 0; i < 5; i++)
            {
                bookDto.Copies.Add(new NewCopyDTO { Number = $"Copy{i + 1}" });
            }

            _ = await mapper.MapAsync<NewBookDTO, Book>(bookDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            book = await databaseContext.Set<Book>().Include(b => b.Copies).FirstAsync();
            Assert.Equal(BookName, book.Name);
            for (var i = 0; i < 5; i++)
            {
                Assert.Contains(book.Copies, c => string.Equals($"Copy{i + 1}", c.Number));
            }
        });

        // map from book to dto
        var bookDto = mapper.Map<Book, BookDTO>(book);
        Assert.Equal(BookName , bookDto.Name);
        for (var i = 0; i < 5; i++)
        {
            Assert.Contains(book.Copies, c => string.Equals($"Copy{i + 1}", c.Number));
        }
    }

    private async Task AddAndUpateBorrower(IMapper mapper, string address1, string? assertAddress1, string? assertAddress2, string address3, string? assertAddress3)
    {
        // create new book
        const string BorrowerName = "Borrower 1";
        
        Borrower borrower = null!;
        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            var borrowerDto = new NewBorrowerDTO { IdentityNumber = "Identity1", Name = BorrowerName, Contact = new NewContactDTO { PhoneNumber = "12345678", Address = address1 } };
            _ = await mapper.MapAsync<NewBorrowerDTO, Borrower>(borrowerDto, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
            Assert.Equal(BorrowerName, borrower.Name);
            Assert.Equal(assertAddress1, borrower.Contact.Address);
        });

        // update existint book dto
        const string UpdatedBorrowerName = "Updated Borrower 1";
        var updateBorrowerDto = mapper.Map<Borrower, UpdateBorrowerDTO>(borrower);
        updateBorrowerDto.Name = UpdatedBorrowerName;
        Assert.Equal(assertAddress2, updateBorrowerDto.Contact.Address);
        updateBorrowerDto.Contact.Address = address3;

        await ExecuteWithNewDatabaseContext(async databaseContext =>
        {
            _ = await mapper.MapAsync<UpdateBorrowerDTO, Borrower>(updateBorrowerDto, databaseContext, b => b.Include(b => b.Contact));
            _ = await databaseContext.SaveChangesAsync();
            borrower = await databaseContext.Set<Borrower>().Include(b => b.Contact).FirstAsync();
            Assert.Equal(UpdatedBorrowerName, borrower.Name);
            Assert.Equal(assertAddress3, borrower.Contact.Address);
        });
    }
}
