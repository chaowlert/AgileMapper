﻿namespace AgileObjects.AgileMapper.Configuration
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using Extensions;
#if NET_STANDARD
    using System.Reflection;
#endif
    using Members;
    using ObjectPopulation;
    using ReadableExpressions;
    using NetStandardPolyfills;

    internal class MappingConfigInfo
    {
        private static readonly Type _allSourceTypes = typeof(MappingConfigInfo);
        private static readonly MappingRuleSet _allRuleSets = new MappingRuleSet("*", true, null, null, null);

        private MappingRuleSet _mappingRuleSet;
        private Type _sourceValueType;
        private ConfiguredLambdaInfo _conditionLambda;
        private bool _negateCondition;

        public MappingConfigInfo(MapperContext mapperContext)
        {
            MapperContext = mapperContext;
        }

        #region Factory Methods

        public static MappingConfigInfo AllRuleSetsAndSourceTypes(MapperContext mapperContext)
            => new MappingConfigInfo(mapperContext).ForAllRuleSets().ForAllSourceTypes();

        public static MappingConfigInfo AllRuleSetsSourceTypesAndTargetTypes(MapperContext mapperContext)
            => AllRuleSetsAndSourceTypes(mapperContext).ForAllTargetTypes();

        #endregion

        public MapperContext MapperContext { get; }

        public Type SourceType { get; private set; }

        public MappingConfigInfo ForAllSourceTypes() => ForSourceType(_allSourceTypes);

        public MappingConfigInfo ForSourceType<TSource>() => ForSourceType(typeof(TSource));

        public MappingConfigInfo ForSourceType(Type sourceType)
        {
            SourceType = sourceType;
            return this;
        }

        public bool HasSameSourceTypeAs(MappingConfigInfo otherConfigInfo) => otherConfigInfo.SourceType == SourceType;

        public bool IsForSourceType(MappingConfigInfo otherConfigInfo) => IsForSourceType(otherConfigInfo.SourceType);

        private bool IsForSourceType(Type sourceType)
            => (SourceType == _allSourceTypes) || SourceType.IsAssignableFrom(sourceType);

        public Type TargetType { get; private set; }

        public MappingConfigInfo ForAllTargetTypes() => ForTargetType<object>();

        public MappingConfigInfo ForTargetType<TTarget>() => ForTargetType(typeof(TTarget));

        public MappingConfigInfo ForTargetType(Type targetType)
        {
            TargetType = targetType;
            return this;
        }

        public bool HasSameTargetTypeAs(MappingConfigInfo otherConfigInfo) => TargetType == otherConfigInfo.TargetType;

        public bool HasCompatibleTypes(MappingConfigInfo otherConfigInfo)
            => HasCompatibleTypes(otherConfigInfo.SourceType, otherConfigInfo.TargetType);

        public bool HasCompatibleTypes(IBasicMapperData mapperData)
            => HasCompatibleTypes(mapperData.SourceType, mapperData.TargetType);

        public bool HasCompatibleTypes(Type sourceType, Type targetType)
        {
            return IsForSourceType(sourceType) &&
                (TargetType.IsAssignableFrom(targetType) || targetType.IsAssignableFrom(TargetType));
        }

        public MappingConfigInfo ForAllRuleSets() => ForRuleSet(_allRuleSets);

        public MappingConfigInfo ForRuleSet(string ruleSetName)
        {
            _mappingRuleSet = MapperContext.RuleSets.GetByName(ruleSetName);
            return this;
        }

        public MappingConfigInfo ForRuleSet(MappingRuleSet ruleSet)
        {
            _mappingRuleSet = ruleSet;
            return this;
        }

        public bool IsFor(MappingRuleSet mappingRuleSet)
            => (_mappingRuleSet == _allRuleSets) || (mappingRuleSet == _mappingRuleSet);

        public MappingConfigInfo ForSourceValueType<TSourceValue>() => ForSourceValueType(typeof(TSourceValue));

        public MappingConfigInfo ForSourceValueType(Type sourceValueType)
        {
            _sourceValueType = sourceValueType;
            return this;
        }

        public void ThrowIfSourceTypeUnconvertible<TTargetValue>()
            => MapperContext.ValueConverters.ThrowIfUnconvertible(_sourceValueType, typeof(TTargetValue));

        #region Conditions

        public bool ConditionUsesMappingDataObjectParameter
            => HasCondition && _conditionLambda.UsesMappingDataObjectParameter;

        public bool HasCondition => _conditionLambda != null;

        public void AddConditionOrThrow(LambdaExpression conditionLambda)
        {
            ErrorIfConditionHasTypeTest(conditionLambda);
            FixEnumComparisonsIfNecessary(ref conditionLambda);

            _conditionLambda = ConfiguredLambdaInfo.For(conditionLambda);
        }

        private static void ErrorIfConditionHasTypeTest(LambdaExpression conditionLambda)
        {
            if (TypeTestFinder.HasNoTypeTest(conditionLambda))
            {
                return;
            }

            var condition = conditionLambda.Body.ToReadableString();

            throw new MappingConfigurationException(string.Format(
                CultureInfo.InvariantCulture,
                "Instead of type testing in condition '{0}', configure for a more specific source or target type.",
                condition));
        }

        private static void FixEnumComparisonsIfNecessary(ref LambdaExpression conditionLambda)
        {
            conditionLambda = EnumComparisonFixer.Check(conditionLambda);
        }

        public void NegateCondition()
        {
            if (HasCondition)
            {
                _negateCondition = true;
            }
        }

        public Expression GetConditionOrNull(
            IMemberMapperData mapperData,
            CallbackPosition position,
            QualifiedMember targetMember)
        {
            if (!HasCondition)
            {
                return GetTypeCheckConditionOrNull(mapperData);
            }

            var condition = _conditionLambda.GetBody(mapperData, position, targetMember);

            if (_negateCondition)
            {
                condition = Expression.Not(condition);
            }

            var targetCanBeNull = position.IsPriorToObjectCreation(targetMember);

            var conditionNestedAccessesChecks = mapperData
                .GetNestedAccessesIn(condition, targetCanBeNull)
                .GetIsNotDefaultComparisonsOrNull();

            if (conditionNestedAccessesChecks != null)
            {
                condition = Expression.AndAlso(conditionNestedAccessesChecks, condition);
            }

            var typeCheck = GetTypeCheckConditionOrNull(mapperData);

            if (typeCheck != null)
            {
                condition = Expression.AndAlso(typeCheck, condition);
            }

            return condition;
        }

        private Expression GetTypeCheckConditionOrNull(IMemberMapperData mapperData)
        {
            var sourceType = (SourceType == _allSourceTypes) ? typeof(object) : SourceType;
            var contextTypes = new[] { sourceType, TargetType };
            var context = mapperData.GetAppropriateMappingContext(contextTypes);

            if (!TargetType.IsDerivedFrom(context.TargetType))
            {
                return null;
            }

            var contextAccess = mapperData.GetAppropriateMappingContextAccess(contextTypes);

            if (contextAccess == mapperData.MappingDataObject)
            {
                return null;
            }

            var targetAccess = mapperData.GetTargetAccess(contextAccess, TargetType);
            var targetAccessNotNull = targetAccess.GetIsNotDefaultComparison();

            return targetAccessNotNull;
        }

        #endregion

        public IBasicMapperData ToMapperData()
        {
            var dummyTargetMember = QualifiedMember
                .From(Member.RootTarget(TargetType), MapperContext);

            return new BasicMapperData(
                _mappingRuleSet,
                SourceType,
                TargetType,
                dummyTargetMember);
        }

        public MappingConfigInfo Clone()
        {
            return new MappingConfigInfo(MapperContext)
            {
                SourceType = SourceType,
                TargetType = TargetType,
                _sourceValueType = _sourceValueType,
                _mappingRuleSet = _mappingRuleSet
            };
        }

        private class TypeTestFinder : ExpressionVisitor
        {
            private bool TypeTestExists { get; set; }

            public static bool HasNoTypeTest(LambdaExpression lambda)
            {
                var typesFinder = new TypeTestFinder();

                typesFinder.Visit(lambda.Body);

                return !typesFinder.TypeTestExists;
            }

            protected override Expression VisitUnary(UnaryExpression unary)
            {
                if (unary.NodeType == ExpressionType.TypeAs)
                {
                    TypeTestExists = true;
                    return unary;
                }

                return base.VisitUnary(unary);
            }

            protected override Expression VisitTypeBinary(TypeBinaryExpression typeBinary)
            {
                TypeTestExists = true;
                return typeBinary;
            }
        }
    }
}