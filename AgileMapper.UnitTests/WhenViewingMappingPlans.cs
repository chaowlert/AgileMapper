﻿namespace AgileObjects.AgileMapper.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenViewingMappingPlans
    {
        [Fact]
        public void ShouldIncludeASimpleTypeMemberMapping()
        {
            var plan = Mapper
                .GetPlanFor<PublicField<string>>()
                .ToANew<PublicProperty<string>>();

            plan.ShouldContain("publicProperty_String.Value = pfsToPpsData.Source.Value;");
        }

        [Fact]
        public void ShouldIncludeAComplexTypeMemberMapping()
        {
            var plan = Mapper
                .GetPlanFor<PersonViewModel>()
                .ToANew<Person>();

            plan.ShouldContain("person.Name = pvmToPData.Source.Name;");
            plan.ShouldContain("address.Line1 = pvmToPData.Source.AddressLine1;");
        }

        [Fact]
        public void ShouldIncludeASimpleTypeEnumerableMemberMapping()
        {
            var plan = Mapper
                .GetPlanFor<PublicProperty<int[]>>()
                .ToANew<PublicField<IEnumerable<int>>>();

            plan.ShouldContain("sourceInt32Array = ");
            plan.ShouldContain("ICollection<int> targetInt32s = ");
            plan.ShouldContain("publicField_Int32s.Value is ICollection<int>");
            plan.ShouldContain("new List<int>(publicField_Int32s.Value)");
            plan.ShouldContain("targetInt32s.Add(sourceInt32Array[i])");
        }

        [Fact]
        public void ShouldIncludeASimpleTypeMemberConversion()
        {
            var plan = Mapper
                .GetPlanFor<PublicProperty<Guid>>()
                .ToANew<PublicField<string>>();

            plan.ShouldContain("data.Source.Value.ToString(");
        }

        [Fact]
        public void ShouldIncludeAConfiguredExpression()
        {
            using (var mapper = Mapper.CreateNew())
            {
                mapper
                    .WhenMapping
                    .From<Person>()
                    .Over<PersonViewModel>()
                    .Map((p, pvm) => p.Title + " " + p.Name)
                    .To(pvm => pvm.Name);

                var plan = mapper
                    .GetPlanFor<Person>()
                    .Over<PersonViewModel>();

                plan.ShouldContain("personViewModel.Name = pToPvmData.Source.Title + \" \" + pToPvmData.Source.Name");
            }
        }

        [Fact]
        public void ShouldIncludeARootComplexTypeEnumerableMapping()
        {
            var plan = Mapper
                .GetPlanFor<IEnumerable<Person>>()
                .OnTo<IEnumerable<PersonViewModel>>();

            plan.ShouldContain("collectionData.Intersection.ForEach((person, personViewModel, i) =>");
            plan.ShouldContain("persons = collectionData.NewSourceItems");
        }

        [Fact]
        public void ShouldIncludeAComplexTypeEnumerableMemberMapping()
        {
            var plan = Mapper
                .GetPlanFor<IList<PersonViewModel>>()
                .Over<IEnumerable<Person>>();

            plan.ShouldContain("personViewModels = collectionData.NewSourceItems");
            plan.ShouldContain("collectionData.Intersection.ForEach((personViewModel, person, i) =>");
            plan.ShouldContain("collectionData.AbsentTargetItems.ForEach(persons.Remove)");

            plan.ShouldContain("IList<PersonViewModel> -> IEnumerable<Person>");
            plan.ShouldNotContain("PersonViewModel -> Person");  // <- because the mapping is inlined
            plan.ShouldNotContain("PersonViewModel -> Address"); // <- because the mapping is inlined
        }

        [Fact]
        public void ShouldIncludeAMemberWithNoDataSource()
        {
            var plan = Mapper
                .GetPlanFor<PersonViewModel>()
                .OnTo<Person>();

            plan.ShouldContain("// No data source for Title");
        }

        [Fact]
        public void ShouldIncludeAnIgnoredMember()
        {
            using (var mapper = Mapper.CreateNew())
            {
                mapper
                    .WhenMapping
                    .To<PersonViewModel>()
                    .Ignore(pvm => pvm.AddressLine1);

                var plan = mapper
                    .GetPlanFor<Person>()
                    .ToANew<PersonViewModel>();

                plan.ShouldContain("// AddressLine1 is ignored");
            }
        }

        [Fact]
        public void ShouldNotIncludeASourceMemberWithTheSameConditionAsAConfiguredMember()
        {
            using (var mapper = Mapper.CreateNew())
            {
                mapper
                    .WhenMapping
                    .From<PublicField<PublicField<string>>>()
                    .ToANew<PublicProperty<string>>()
                    .Map((pf, pp) => pf.Value.Value)
                    .To(pp => pp.Value);

                var plan = mapper
                    .GetPlanFor<PublicField<PublicField<string>>>()
                    .ToANew<PublicProperty<string>>();

                plan.ShouldContain("data.Source.Value.Value");
                plan.ShouldNotContain("data.Source.Value.ToString()");
            }
        }

        [Fact]
        public void ShouldShowInlineMappers()
        {
            var plan = Mapper
                .GetPlanFor<PublicField<PublicField<PublicField<int>>>>()
                .ToANew<PublicField<PublicSealed<PublicSealed<long>>>>();

            var numberOfHeaders = Regex.Matches(plan, "// Map ").Count;

            numberOfHeaders.ShouldBe(1);
        }

        [Fact]
        public void ShouldShowMapChildCalls()
        {
            var plan = Mapper
                .GetPlanFor<PublicProperty<object>>()
                .ToANew<PublicSetMethod<Customer>>();

            plan.ShouldContain("// Map PublicProperty<object> -> PublicSetMethod<Customer>");
            plan.ShouldContain("// Map object -> Customer");
            plan.ShouldContain("// Map object -> Address");
            plan.ShouldContain("ppoToPsmcData.Map(");
        }

        [Fact]
        public void ShouldShowNestedMapChildCalls()
        {
            var plan = Mapper
                .GetPlanFor<PublicProperty<PublicField<object>>>()
                .ToANew<PublicSetMethod<PublicProperty<Order>>>();

            plan.ShouldContain("// Map PublicProperty<PublicField<object>> -> PublicSetMethod<PublicProperty<Order>>");
            plan.ShouldNotContain("// Map PublicField<object> -> PublicProperty<Order>");
            plan.ShouldContain("// Map object -> Order");
            plan.ShouldContain("pfoToPpoData.Map(");
        }

        [Fact]
        public void ShouldShowMapElementCalls()
        {
            var plan = Mapper
                .GetPlanFor<PublicProperty<object[]>>()
                .ToANew<PublicSetMethod<ICollection<Product>>>();

            plan.ShouldContain("// Map PublicProperty<object[]> -> PublicSetMethod<ICollection<Product>>");
            plan.ShouldContain("// Map object -> Product");
            plan.ShouldContain("products.Add(oaToPsData.Map(objectArray[i]");
        }
    }
}
