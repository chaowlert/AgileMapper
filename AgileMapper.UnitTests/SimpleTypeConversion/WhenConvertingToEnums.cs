﻿namespace AgileObjects.AgileMapper.UnitTests.SimpleTypeConversion
{
    using Shouldly;
    using TestClasses;
    using Xunit;

    public class WhenConvertingToEnums
    {
        [Fact]
        public void ShouldMapAByteToAnEnum()
        {
            var source = new PublicField<byte> { Value = (byte)Title.Dr };
            var result = Mapper.Map(source).ToANew<PublicField<Title>>();

            result.Value.ShouldBe(Title.Dr);
        }

        [Fact]
        public void ShouldMapAShortToAnEnum()
        {
            var source = new PublicField<short> { Value = (short)Title.Miss };
            var result = Mapper.Map(source).ToANew<PublicField<Title>>();

            result.Value.ShouldBe(Title.Miss);
        }

        [Fact]
        public void ShouldMapANullableIntToAnEnum()
        {
            var source = new PublicProperty<int?> { Value = (int)Title.Lady };
            var result = Mapper.Map(source).ToANew<PublicField<Title>>();

            result.Value.ShouldBe(Title.Lady);
        }

        [Fact]
        public void ShouldMapAnIntToAnEnum()
        {
            var source = new PublicProperty<int> { Value = (int)Title.Dr };
            var result = Mapper.Map(source).ToANew<PublicField<Title>>();

            result.Value.ShouldBe((Title)source.Value);
        }

        [Fact]
        public void ShouldMapANullNullableIntToAnEnum()
        {
            var source = new PublicProperty<int?> { Value = null };
            var result = Mapper.Map(source).ToANew<PublicField<TitleShortlist>>();

            result.Value.ShouldBeDefault();
        }

        [Fact]
        public void ShouldMapALongToAnEnum()
        {
            var source = new PublicProperty<long> { Value = (long)Title.Miss };
            var result = Mapper.Map(source).ToANew<PublicField<Title>>();

            result.Value.ShouldBe((Title)source.Value);
        }

        [Fact]
        public void ShouldMapANonMatchingNullableLongToANullableEnum()
        {
            var source = new PublicProperty<long?> { Value = (long)Title.Earl };
            var result = Mapper.Map(source).ToANew<PublicField<TitleShortlist?>>();

            result.Value.ShouldBeNull();
        }

        [Fact]
        public void ShouldMapAMatchingCharacterOnToAnEnum()
        {
            var source = new PublicField<char> { Value = '6' };
            var result = Mapper.Map(source).OnTo(new PublicProperty<TitleShortlist>());

            result.Value.ShouldBe((TitleShortlist)6);
        }

        [Fact]
        public void ShouldMapANonMatchingNullableCharacterOnToANullableEnum()
        {
            var source = new PublicField<char?> { Value = 'x' };
            var result = Mapper.Map(source).OnTo(new PublicProperty<TitleShortlist?>());

            result.Value.ShouldBeDefault();
        }

        [Fact]
        public void ShouldMapAMatchingNullableCharacterOnToANullableEnum()
        {
            var source = new PublicField<char?> { Value = '2' };
            var result = Mapper.Map(source).OnTo(new PublicProperty<Title?>());

            result.Value.ShouldBe((Title)2);
        }

        [Fact]
        public void ShouldMapAMatchingStringOnToAnEnum()
        {
            var source = new PublicField<string> { Value = Title.Mrs.ToString() };
            var result = Mapper.Map(source).OnTo(new PublicProperty<Title>());

            result.Value.ShouldBe(Title.Mrs);
        }

        [Fact]
        public void ShouldMapAMatchingStringOnToAnEnumCaseInsensitively()
        {
            var source = new PublicField<string> { Value = Title.Miss.ToString().ToLowerInvariant() };
            var result = Mapper.Map(source).OnTo(new PublicProperty<Title>());

            result.Value.ShouldBe(Title.Miss);
        }

        [Fact]
        public void ShouldMapAMatchingNumericStringOverAnEnum()
        {
            var source = new PublicField<string> { Value = ((int)Title.Dr).ToString() };
            var result = Mapper.Map(source).Over(new PublicProperty<Title>());

            result.Value.ShouldBe(Title.Dr);
        }

        [Fact]
        public void ShouldMapANonMatchingStringOnToAnEnum()
        {
            var source = new PublicField<string> { Value = "ihdfsjsda" };
            var result = Mapper.Map(source).OnTo(new PublicProperty<Title>());

            result.Value.ShouldBeDefault();
        }

        [Fact]
        public void ShouldMapANonMatchingStringToANullableEnum()
        {
            var source = new PublicProperty<string> { Value = "ytej" };
            var result = Mapper.Map(source).ToANew<PublicProperty<Title?>>();

            result.Value.ShouldBeNull();
        }

        [Fact]
        public void ShouldMapAnEnumToAnEnum()
        {
            var source = new PublicProperty<TitleShortlist> { Value = TitleShortlist.Mrs };
            var result = Mapper.Map(source).ToANew<PublicProperty<Title>>();

            result.Value.ShouldBe(Title.Mrs);
        }

        [Fact]
        public void ShouldMapANonMatchingEnumToANullableEnum()
        {
            var source = new PublicProperty<Title> { Value = Title.Lord };
            var result = Mapper.Map(source).ToANew<PublicProperty<TitleShortlist?>>();

            result.Value.ShouldBeNull();
        }

        [Fact]
        public void ShouldMapANullNullableEnumToAnEnum()
        {
            var source = new PublicProperty<Title?> { Value = null };
            var result = Mapper.Map(source).ToANew<PublicProperty<Title>>();

            result.Value.ShouldBeDefault();
        }

        [Fact]
        public void ShouldMapEnumsConditionally()
        {
            using (var mapper = Mapper.CreateNew())
            {
                mapper.WhenMapping
                    .From<PublicTwoFields<Title?, Title>>()
                    .To<PublicField<TitleShortlist>>()
                    .Map((ptf, pf) => ptf.Value1).To(pf => pf.Value)
                    .But
                    .If((ptf, pf) => ptf.Value1 == null)
                    .Map((ptf, pf) => ptf.Value2).To(pf => pf.Value)
                    .And
                    .If((ptf, pf) => Title.Duke == ptf.Value1)
                    .Map(TitleShortlist.Other).To(pf => pf.Value);

                var nonNullSource = new PublicTwoFields<Title?, Title> { Value1 = Title.Dr, Value2 = Title.Count };
                var nonNullResult = mapper.Map(nonNullSource).ToANew<PublicField<TitleShortlist>>();

                nonNullResult.Value.ShouldBe(TitleShortlist.Dr);

                var nullSource = new PublicTwoFields<Title?, Title> { Value1 = null, Value2 = Title.Mrs };
                var nullResult = mapper.Map(nullSource).ToANew<PublicField<TitleShortlist>>();

                nullResult.Value.ShouldBe(TitleShortlist.Mrs);

                var dukeSource = new PublicTwoFields<Title?, Title> { Value1 = Title.Duke };
                var dukeResult = mapper.Map(dukeSource).ToANew<PublicField<TitleShortlist>>();

                dukeResult.Value.ShouldBe(TitleShortlist.Other);
            }
        }
    }
}
