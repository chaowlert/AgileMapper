﻿namespace AgileObjects.AgileMapper.UnitTests.Configuration
{
    using AgileMapper.Configuration;
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenConfiguringDerivedTypes
    {
        [Fact]
        public void ShouldMapACustomTypePair()
        {
            using (var mapper = Mapper.CreateNew())
            {
                mapper.WhenMapping
                    .From<Product>()
                    .To<ProductDto>()
                    .Map<MegaProduct>()
                    .To<ProductDtoMega>();

                Product source = new MegaProduct { ProductId = "PrettyDarnMega", HowMega = 1.00m };
                var result = mapper.Map(source).ToANew<ProductDto>();

                result.ShouldBeOfType<ProductDtoMega>();
                result.ProductId.ShouldBe("PrettyDarnMega");
                ((ProductDtoMega)result).HowMega.ShouldBe("1.00");
            }
        }

        [Fact]
        public void ShouldMapADerivedTypePairConditionally()
        {
            using (var mapper = Mapper.CreateNew())
            {
                var exampleInstance = new { Name = default(string), Discount = default(decimal?), Report = default(string) };

                mapper
                    .WhenMapping
                    .From(exampleInstance)
                    .ToANew<PersonViewModel>()
                    .If(s => s.Source.Discount.HasValue)
                    .MapTo<CustomerViewModel>()
                    .And
                    .If(x => !string.IsNullOrWhiteSpace(x.Source.Report))
                    .MapTo<MysteryCustomerViewModel>();

                var mysteryCustomerSource = new
                {
                    Name = "???",
                    Discount = (decimal?).5m,
                    Report = "Lovely!"
                };

                var mysteryCustomerResult = mapper.Map(mysteryCustomerSource).ToANew<PersonViewModel>();

                mysteryCustomerResult.ShouldBeOfType<MysteryCustomerViewModel>();
                mysteryCustomerResult.Name.ShouldBe("???");
                ((CustomerViewModel)mysteryCustomerResult).Discount.ShouldBe(0.5);
                ((MysteryCustomerViewModel)mysteryCustomerResult).Report.ShouldBe("Lovely!");

                var customerSource = new
                {
                    Name = "Firsty",
                    Discount = (decimal?)1,
                    Report = string.Empty
                };

                var customerResult = mapper.Map(customerSource).ToANew<PersonViewModel>();

                customerResult.ShouldBeOfType<CustomerViewModel>();
                customerResult.Name.ShouldBe("Firsty");
                ((CustomerViewModel)customerResult).Discount.ShouldBe(1.0);

                var personSource = new
                {
                    Name = "Datey",
                    Discount = default(decimal?),
                    Report = default(string)
                };

                var personResult = mapper.Map(personSource).ToANew<PersonViewModel>();

                personResult.ShouldBeOfType<PersonViewModel>();
                personResult.Name.ShouldBe("Datey");
            }
        }

        [Fact]
        public void ShouldErrorIfSameSourceTypeSpecified()
        {
            var pairingEx = Should.Throw<MappingConfigurationException>(() =>
            {
                using (var mapper = Mapper.CreateNew())
                {
                    mapper.WhenMapping
                        .From<Product>()
                        .To<ProductDto>()
                        .Map<Product>()
                        .To<ProductDtoMega>();

                }
            });

            pairingEx.Message.ShouldContain("derived source type must be specified");
        }

        [Fact]
        public void ShouldErrorIfSameTargetTypeSpecified()
        {
            var pairingEx = Should.Throw<MappingConfigurationException>(() =>
            {
                using (var mapper = Mapper.CreateNew())
                {
                    mapper.WhenMapping
                        .From<Product>()
                        .To<ProductDto>()
                        .Map<MegaProduct>()
                        .To<ProductDto>();

                }
            });

            pairingEx.Message.ShouldContain("derived target type must be specified");
        }

        [Fact]
        public void ShouldErrorIfUnnecessaryPairingSpecified()
        {
            var pairingEx = Should.Throw<MappingConfigurationException>(() =>
            {
                using (var mapper = Mapper.CreateNew())
                {
                    mapper.WhenMapping
                        .From<Person>()
                        .To<PersonViewModel>()
                        .Map<Customer>()
                        .To<CustomerViewModel>();

                }
            });

            pairingEx.Message.ShouldContain("Customer is automatically mapped to CustomerViewModel");
            pairingEx.Message.ShouldContain("when mapping Person to PersonViewModel");
            pairingEx.Message.ShouldContain("does not need to be configured");
        }
    }
}
