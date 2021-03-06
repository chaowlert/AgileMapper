﻿namespace AgileObjects.AgileMapper.UnitTests
{
    using System;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenMappingOverComplexTypes
    {
        [Fact]
        public void ShouldReuseAnExistingTargetObject()
        {
            var source = new PublicField<string>();
            var target = new PublicProperty<string>();

            var result = Mapper.Map(source).Over(target);

            result.ShouldBe(target);
        }

        [Fact]
        public void ShouldMapFromAnAnonymousType()
        {
            var source = new { Id = Guid.NewGuid(), Name = "Mr Pants" };
            var target = new Person { Id = Guid.NewGuid(), Name = "Mrs Trousers" };
            var result = Mapper.Map(source).Over(target);

            result.Id.ShouldBe(source.Id);
            result.Name.ShouldBe(source.Name);
        }

        [Fact]
        public void ShouldOverwriteAnExistingSimpleTypePropertyValue()
        {
            var source = new PublicField<int> { Value = 123 };
            var target = new PublicProperty<int> { Value = 789 };

            Mapper.Map(source).Over(target);

            target.Value.ShouldBe(source.Value);
        }

        [Fact]
        public void ShouldNullAnExistingSimpleTypePropertyValue()
        {
            var source = new PublicProperty<double?> { Value = null };
            var target = new PublicField<double?> { Value = 537.0 };

            Mapper.Map(source).Over(target);

            target.Value.ShouldBeNull();
        }

        [Fact]
        public void ShouldHandleANullSourceObject()
        {
            var target = new PublicProperty<int>();
            var result = Mapper.Map(default(PublicField<int>)).Over(target);

            result.ShouldBe(target);
        }
    }
}
