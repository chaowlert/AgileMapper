﻿namespace AgileObjects.AgileMapper.UnitTests
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenMappingToNewEnumerables
    {
        [Fact]
        public void ShouldCreateASimpleTypeArray()
        {
            var source = new[] { 1, 2, 3 };
            var result = Mapper.Map(source).ToANew<int[]>();

            result.ShouldNotBeNull();
            result.ShouldNotBeSameAs(source);
            result.SequenceEqual(source).ShouldBeTrue();
        }

        [Fact]
        public void ShouldCreateASimpleTypeList()
        {
            var source = new[] { 'O', 'M', 'G' };
            var result = Mapper.Map(source).ToANew<List<char>>();

            result.ShouldNotBeNull();
            result.SequenceEqual(source).ShouldBeTrue();
        }

        [Fact]
        public void ShouldCreateAConvertedSimpleTypeCollection()
        {
            var source = new List<string> { "1", "2", "3" };
            var result = Mapper.Map(source).ToANew<Collection<byte?>>();

            result.ShouldNotBeNull();
            result.ShouldNotBeSameAs(source);
            result.ShouldBe<byte?>(1, 2, 3);
        }

        [Fact]
        public void ShouldCreateASimpleTypeEnumerable()
        {
            var source = new List<string> { "One", "Two", "Three" };
            var result = Mapper.Map(source).ToANew<IEnumerable<string>>();

            result.ShouldNotBeNull();
            result.ShouldNotBeSameAs(source);
            result.SequenceEqual(source).ShouldBeTrue();
        }

        [Fact]
        public void ShouldCreateAComplexTypeList()
        {
            var source = new List<Person>
            {
                new Person { Name = "Pete", Address = new Address() },
                new Person { Name = "Johnny", Address = new Address() }
            };

            var result = Mapper.Map(source).ToANew<List<Person>>();

            result.ShouldNotBeNull();
            result.ShouldNotBe(source);
            result.First().ShouldNotBe(source.First());
            result.First().Name.ShouldBe(source.First().Name);
            result.Last().ShouldNotBe(source.Last());
            result.Last().Name.ShouldBe(source.Last().Name);
        }

        [Fact]
        public void ShouldHandleANullComplexTypeElement()
        {
            var source = new List<Product>
            {
                new Product { ProductId = "Surprise" },
                null,
                new Product { ProductId = "Boomstick" }
            };

            var result = Mapper.Map(source).ToANew<List<Product>>();

            result.ShouldNotBeNull();
            result.ShouldNotBe(source);
            result.Second().ShouldBeNull();
        }

        [Fact]
        public void ShouldCreateAnEmptyListByDefault()
        {
            var source = new PublicProperty<string>();
            var result = Mapper.Map(source).ToANew<List<Person>>();

            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }
    }
}
