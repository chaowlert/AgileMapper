﻿namespace AgileObjects.AgileMapper.UnitTests.Configuration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenConfiguringObjectCreationCallbacks
    {
        [Fact]
        public void ShouldCallAnObjectCreatedCallback()
        {
            using (var mapper = Mapper.Create())
            {
                var createdInstance = default(PublicProperty<int>);

                mapper.After
                    .CreatingInstances
                    .Call(ctx => createdInstance = (PublicProperty<int>)ctx.Object);

                var source = new PublicField<int>();
                var result = mapper.Map(source).ToNew<PublicProperty<int>>();

                createdInstance.ShouldNotBeNull();
                createdInstance.ShouldBe(result);
            }
        }

        [Fact]
        public void ShouldWrapAnObjectCreatedCallbackException()
        {
            Assert.Throws<MappingException>(() =>
            {
                using (var mapper = Mapper.Create())
                {
                    mapper.After
                        .CreatingInstances
                        .Call(ctx => { throw new InvalidOperationException(); });

                    mapper.Map(new PublicProperty<int>()).ToNew<PublicField<int>>();
                }
            });
        }

        [Fact]
        public void ShouldWrapANestedObjectCreatingCallbackException()
        {
            Assert.Throws<MappingException>(() =>
            {
                using (var mapper = Mapper.Create())
                {
                    mapper
                        .After
                        .CreatingInstancesOf<Address>()
                        .Call(ctx => { throw new InvalidOperationException("OH NO"); });

                    mapper.Map(new PersonViewModel { AddressLine1 = "My House" }).ToNew<Person>();
                }
            });
        }

        [Fact]
        public void ShouldCallAnObjectCreatedCallbackForASpecifiedType()
        {
            using (var mapper = Mapper.Create())
            {
                var createdPerson = default(Person);

                mapper.After
                    .CreatingInstancesOf<Person>()
                    .Call((s, t, o) => createdPerson = o);

                var nonMatchingSource = new { Value = "12345" };
                var nonMatchingResult = mapper.Map(nonMatchingSource).ToNew<PublicProperty<int>>();

                createdPerson.ShouldBeDefault();
                nonMatchingResult.Value.ShouldBe(12345);

                var matchingSource = new Person { Name = "Alex" };
                var matchingResult = mapper.Map(matchingSource).ToNew<Person>();

                createdPerson.ShouldNotBeNull();
                createdPerson.ShouldBe(matchingResult);
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatedCallbackForSpecifiedSourceAndTargetTypes()
        {
            using (var mapper = Mapper.Create())
            {
                var createdPerson = default(Person);

                mapper.WhenMapping
                    .From<PersonViewModel>()
                    .To<Person>()
                    .After
                    .CreatingTargetInstances
                    .Call(ctx => createdPerson = ctx.Object);

                var nonMatchingSource = new { Name = "Harry" };
                var nonMatchingResult = mapper.Map(nonMatchingSource).ToNew<Person>();

                createdPerson.ShouldBeNull();
                nonMatchingResult.Name.ShouldBe("Harry");

                var matchingSource = new PersonViewModel { Name = "Tom" };
                var matchingResult = mapper.Map(matchingSource).ToNew<Person>();

                createdPerson.ShouldNotBeNull();
                createdPerson.ShouldBe(matchingResult);
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatedCallbackForSpecifiedSourceTargetAndCreatedTypes()
        {
            using (var mapper = Mapper.Create())
            {
                var createdAddress = default(Address);

                mapper.WhenMapping
                    .From<PersonViewModel>()
                    .To<Person>()
                    .After
                    .CreatingInstancesOf<Address>()
                    .Call(ctx => createdAddress = ctx.Object);

                var nonMatchingSource = new { Address = new Address { Line1 = "Blah" } };
                var nonMatchingResult = mapper.Map(nonMatchingSource).ToNew<Person>();

                createdAddress.ShouldBeNull();
                nonMatchingResult.Address.Line1.ShouldBe("Blah");

                var matchingSource = new PersonViewModel { AddressLine1 = "Bleh" };
                var matchingResult = mapper.Map(matchingSource).ToNew<Person>();

                createdAddress.ShouldNotBeNull();
                createdAddress.ShouldBe(matchingResult.Address);
                matchingResult.Address.Line1.ShouldBe("Bleh");
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatedCallbackWithASourceObject()
        {
            using (var mapper = Mapper.Create())
            {
                mapper.WhenMapping
                    .From<PersonViewModel>()
                    .OnTo<Person>()
                    .After
                    .CreatingInstancesOf<Address>()
                    .Call(ctx => ctx.Object.Line2 = ctx.Source.Name);

                var nonMatchingSource = new { Name = "Wilma" };
                var nonMatchingTarget = new Person { Name = "Fred" };
                var nonMatchingResult = mapper.Map(nonMatchingSource).OnTo(nonMatchingTarget);

                nonMatchingResult.Address.Line2.ShouldBeNull();
                nonMatchingResult.Name.ShouldBe("Fred");

                var matchingSource = new PersonViewModel { Name = "Betty" };
                var matchingTarget = new Person { Name = "Fred" };
                var matchingResult = mapper.Map(matchingSource).OnTo(matchingTarget);

                matchingResult.Address.Line2.ShouldBe("Betty");
                matchingResult.Name.ShouldBe("Fred");
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatedCallbackConditionally()
        {
            using (var mapper = Mapper.Create())
            {
                var createdInstanceTypes = new List<Type>();

                mapper.WhenMapping
                    .ToANew<Person>()
                    .After
                    .CreatingInstances
                    .If(ctx => ctx.Object is Address)
                    .Call(ctx => createdInstanceTypes.Add(ctx.Object.GetType()));

                var source = new { Name = "Homer", AddressLine1 = "Springfield" };
                var nonMatchingResult = mapper.Map(source).ToNew<PersonViewModel>();

                createdInstanceTypes.ShouldBeEmpty();
                nonMatchingResult.Name.ShouldBe("Homer");

                var matchingResult = mapper.Map(source).ToNew<Person>();

                createdInstanceTypes.ShouldBe(new[] { typeof(Address) });
                matchingResult.Name.ShouldBe("Homer");
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatingCallbackWithASourceObject()
        {
            using (var mapper = Mapper.Create())
            {
                mapper.WhenMapping
                    .From<Person>()
                    .ToANew<PersonViewModel>()
                    .Before
                    .CreatingTargetInstances
                    .Call(ctx => ctx.Source.Name += $"! {ctx.Source.Name}!");

                var nonMatchingSource = new { Name = "Lester" };
                var nonMatchingResult = mapper.Map(nonMatchingSource).ToNew<PersonViewModel>();

                nonMatchingResult.Name.ShouldBe("Lester");

                var matchingSource = new Person { Name = "Carolin" };
                var matchingResult = mapper.Map(matchingSource).ToNew<PersonViewModel>();

                matchingResult.Name.ShouldBe("Carolin! Carolin!");
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatingCallbackWithASourceAndTargetConditionally()
        {
            using (var mapper = Mapper.Create())
            {
                mapper.WhenMapping
                    .From<PersonViewModel>()
                    .OnTo<Person>()
                    .After
                    .CreatingInstances
                    .If((pvm, p, i) => (i - 1) == 0)
                    .Call((pvm, p, o) => p.Name += " + " + pvm.Name);

                var source = new[]
                {
                    new PersonViewModel { Id = Guid.NewGuid(), Name = "Mac", AddressLine1 = "Blah" },
                    new PersonViewModel { Id = Guid.NewGuid(), Name = "Dennis", AddressLine1 = "Jah" }
                };

                var target = new[]
                {
                    new Person { Id = source.First().Id, Name = "Dee" },
                    new Person { Id = source.Second().Id, Name = "Charlie" }
                };

                var result = mapper.Map(source).OnTo(target);

                result.First().Name.ShouldBe("Dee");
                result.Second().Name.ShouldBe("Charlie + Dennis");
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatingCallbackInARootEnumerableConditionally()
        {
            using (var mapper = Mapper.Create())
            {
                var createdAddressesByIndex = new Dictionary<int, string>();

                mapper.WhenMapping
                    .From<PersonViewModel>()
                    .ToANew<Person>()
                    .Before
                    .CreatingInstancesOf<Address>()
                    .If((s, t, i) => (i == 1) || (i == 2))
                    .Call(ctx => createdAddressesByIndex[ctx.EnumerableIndex.GetValueOrDefault()] = ctx.Source.AddressLine1);

                var source = new[]
                {
                    new PersonViewModel { AddressLine1 = "Zero!" },
                    new PersonViewModel { AddressLine1 = "One!" },
                    new PersonViewModel { AddressLine1 = "Two!" }
                };

                var result = mapper.Map(source).ToNew<Person[]>();

                result.Select(p => p.Address.Line1).ShouldBe("Zero!", "One!", "Two!");

                createdAddressesByIndex.ShouldNotContainKey(0);
                createdAddressesByIndex.ShouldContainKeyAndValue(1, "One!");
                createdAddressesByIndex.ShouldContainKeyAndValue(2, "Two!");
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatingCallbackInAMemberEnumerable()
        {
            using (var mapper = Mapper.Create())
            {
                var sourceObjectTypesByIndex = new Dictionary<int, Type>();

                mapper.WhenMapping
                    .From<Person>()
                    .ToANew<PersonViewModel>()
                    .Before
                    .CreatingInstances
                    .Call((p, pvm, i) => sourceObjectTypesByIndex[i.GetValueOrDefault()] = p.GetType());

                var source = new[] { new Person(), new Customer() };
                var result = mapper.Map(source).ToNew<ICollection<PersonViewModel>>();

                result.Count.ShouldBe(2);
                sourceObjectTypesByIndex.ShouldContainKeyAndValue(0, typeof(Person));
                sourceObjectTypesByIndex.ShouldContainKeyAndValue(1, typeof(Customer));
            }
        }

        [Fact]
        public void ShouldCallAnObjectCreatingCallbackInAMemberCollectionConditionally()
        {
            using (var mapper = Mapper.Create())
            {
                var sourceAddressesByIndex = new Dictionary<int, Address>();

                mapper.WhenMapping
                    .From<Person>()
                    .Over<Customer>()
                    .After
                    .CreatingInstances
                    .If((p, c, o, i) => (o is Address) && (i >= 1))
                    .Call((p, c, o, i) => sourceAddressesByIndex[i.GetValueOrDefault()] = (Address)o);

                var source = new PublicField<Collection<Person>>
                {
                    Value = new Collection<Person>
                    {
                        new Person { Id = Guid.NewGuid(), Name = "Person 0" },
                        new Person { Id = Guid.NewGuid(), Name = "Person 1", Address = new Address { Line1 = "My house" } }
                    }
                };
                var target = new PublicProperty<IEnumerable>
                {
                    Value = new[]
                    {
                        new Person { Id = source.Value.First().Id },
                        new Customer { Id = source.Value.Second().Id }
                    }
                };
                var result = mapper.Map(source).Over(target);
                var resultItems = result.Value.Cast<Person>().ToArray();

                resultItems.Length.ShouldBe(2);
                sourceAddressesByIndex.Count.ShouldBe(1);
                sourceAddressesByIndex.ShouldContainKeyAndValue(1, resultItems.Second().Address);
            }
        }
    }
}
