﻿namespace AgileObjects.AgileMapper.UnitTests.Extensions
{
    using System.Collections.Generic;
    using AgileMapper.Extensions;
    using TestClasses;
    using Xunit;

    public class WhenGeneratingVariableNames
    {
        [Fact]
        public void ShouldNameAShortVariableForACollectionType()
        {
            typeof(ICollection<Person>).GetShortVariableName().ShouldBe("ps");
        }

        [Fact]
        public void ShouldNameAMultiLetterShortVariableForACollectionType()
        {
            typeof(ICollection<CustomerViewModel>).GetShortVariableName().ShouldBe("cvms");
        }

        [Fact]
        public void ShouldNameAVariableForAnArrayType()
        {
            typeof(Box[]).GetVariableNameInCamelCase().ShouldBe("boxArray");
        }

        [Fact]
        public void ShouldNameAVariableForACollectionTypeEndingInX()
        {
            typeof(ICollection<Box>).GetVariableNameInCamelCase().ShouldBe("boxes");
        }

        [Fact]
        public void ShouldNameAVariableForAnEnumerableTypeEndingInZ()
        {
            typeof(IEnumerable<Fuzz>).GetVariableNameInPascalCase().ShouldBe("Fuzzes");
        }

        [Fact]
        public void ShouldNameAVariableForAnEnumerableTypeEndingInDoubleS()
        {
            typeof(IEnumerable<Glass>).GetVariableNameInPascalCase().ShouldBe("Glasses");
        }

        [Fact]
        public void ShouldNameAVariableForAListTypeEndingInCh()
        {
            typeof(List<Church>).GetVariableNameInCamelCase().ShouldBe("churches");
        }

        [Fact]
        public void ShouldNameAVariableForAListTypeEndingInSh()
        {
            typeof(List<Hush>).GetVariableNameInCamelCase().ShouldBe("hushes");
        }

        [Fact]
        public void ShouldNameAVariableForAListTypeEndingInVowelY()
        {
            typeof(List<Journey>).GetVariableNameInCamelCase().ShouldBe("journeys");
        }

        [Fact]
        public void ShouldNameAVariableForAnIListTypeEndingInConsonantY()
        {
            typeof(IList<Body>).GetVariableNameInPascalCase().ShouldBe("Bodies");
        }

        // ReSharper disable ClassNeverInstantiated.Local
        private class Box { }

        private class Fuzz { }

        private class Glass { }

        private class Church { }

        private class Hush { }

        private class Journey { }

        private class Body { }
        // ReSharper restore ClassNeverInstantiated.Local
    }
}
