﻿namespace AgileObjects.AgileMapper.DataSources
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Members;
    using ObjectPopulation;

    internal static class InlineMappingFactory
    {
        public static Expression GetDerivedTypeMapping(
            IObjectMappingData declaredTypeMappingData,
            Expression sourceValue,
            Type targetType)
        {
            var declaredTypeMapperData = declaredTypeMappingData.MapperData;

            var targetValue = declaredTypeMapperData.TargetMember.IsReadable
                ? declaredTypeMapperData.TargetObject.GetConversionTo(targetType)
                : Expression.Default(targetType);

            var derivedTypeMappingData = declaredTypeMappingData.WithTypes(sourceValue.Type, targetType);

            if (declaredTypeMappingData.IsRoot)
            {
                return GetDerivedTypeRootMapping(derivedTypeMappingData, sourceValue, targetValue);
            }

            if (declaredTypeMapperData.TargetMember.LeafMember.MemberType == MemberType.EnumerableElement)
            {
                return GetDerivedTypeElementMapping(derivedTypeMappingData, sourceValue, targetValue);
            }

            return GetDerivedTypeChildMapping(derivedTypeMappingData, sourceValue, targetValue);
        }

        private static Expression GetDerivedTypeRootMapping(
            IObjectMappingData derivedTypeMappingData,
            Expression sourceValue,
            Expression targetValue)
        {
            var declaredTypeMapperData = derivedTypeMappingData.DeclaredTypeMappingData.MapperData;
            var derivedTypeMapperData = derivedTypeMappingData.MapperData;
            var derivedTypeMapper = derivedTypeMappingData.Mapper;

            var inlineMappingBlock = GetInlineMappingBlock(
                derivedTypeMapper,
                derivedTypeMapperData,
                MappingDataFactory.ForRootMethod,
                sourceValue,
                targetValue,
                Expression.Property(declaredTypeMapperData.MappingDataObject, "MappingContext"));

            return inlineMappingBlock;
        }

        private static Expression GetDerivedTypeElementMapping(
            IObjectMappingData derivedTypeMappingData,
            Expression sourceElementValue,
            Expression targetElementValue)
        {
            var declaredTypeMapperData = derivedTypeMappingData.DeclaredTypeMappingData.MapperData;

            return GetElementMapping(
                sourceElementValue,
                targetElementValue,
                Expression.Property(declaredTypeMapperData.EnumerableIndex, "Value"),
                derivedTypeMappingData,
                declaredTypeMapperData);
        }

        private static Expression GetDerivedTypeChildMapping(
            IObjectMappingData derivedTypeMappingData,
            Expression sourceValue,
            Expression targetValue)
        {
            var declaredTypeMapperData = derivedTypeMappingData.DeclaredTypeMappingData.MapperData;
            var derivedTypeMapperData = derivedTypeMappingData.MapperData;

            return GetChildMapping(
                derivedTypeMapperData.SourceMember,
                sourceValue,
                targetValue,
                declaredTypeMapperData.EnumerableIndex,
                declaredTypeMapperData.DataSourceIndex,
                derivedTypeMappingData,
                derivedTypeMapperData,
                declaredTypeMapperData);
        }

        public static Expression GetChildMapping(int dataSourceIndex, IMemberMappingData childMappingData)
        {
            var childMapperData = childMappingData.MapperData;
            var relativeMember = childMapperData.SourceMember.RelativeTo(childMapperData.SourceMember);
            var sourceMemberAccess = relativeMember.GetQualifiedAccess(childMapperData.SourceObject);

            return GetChildMapping(
                relativeMember,
                sourceMemberAccess,
                dataSourceIndex,
                childMappingData);
        }

        public static Expression GetChildMapping(
            IQualifiedMember sourceMember,
            Expression sourceMemberAccess,
            int dataSourceIndex,
            IMemberMappingData childMappingData)
        {
            var childMapperData = childMappingData.MapperData;
            var targetMemberAccess = childMapperData.GetTargetMemberAccess();

            return GetChildMapping(
                sourceMember,
                sourceMemberAccess,
                targetMemberAccess,
                childMapperData.Parent.EnumerableIndex,
                dataSourceIndex,
                childMappingData.Parent,
                childMapperData,
                childMapperData.Parent);
        }

        private static Expression GetChildMapping(
            IQualifiedMember sourceMember,
            Expression sourceValue,
            Expression targetValue,
            Expression enumerableIndex,
            int dataSourceIndex,
            IObjectMappingData parentMappingData,
            IMemberMapperData childMapperData,
            ObjectMapperData declaredTypeMapperData)
        {
            var childMappingData = ObjectMappingDataFactory.ForChild(
                sourceMember,
                childMapperData.TargetMember,
                dataSourceIndex,
                parentMappingData);

            if (childMappingData.MapperKey.MappingTypes.RuntimeTypesNeeded)
            {
                return declaredTypeMapperData.GetMapCall(sourceValue, childMapperData.TargetMember, dataSourceIndex);
            }

            if (TargetMemberIsRecursive(childMapperData))
            {
                var mapperFuncCall = GetMapRecursionCallFor(
                    childMappingData,
                    sourceValue,
                    dataSourceIndex,
                    declaredTypeMapperData);

                return mapperFuncCall;
            }

            var childMapper = childMappingData.Mapper;

            var inlineMappingBlock = GetInlineMappingBlock(
                childMapper,
                childMappingData.MapperData,
                MappingDataFactory.ForChildMethod,
                sourceValue,
                targetValue,
                enumerableIndex,
                Expression.Constant(childMapperData.TargetMember.RegistrationName),
                Expression.Constant(dataSourceIndex),
                declaredTypeMapperData.MappingDataObject);

            return inlineMappingBlock;
        }

        private static bool TargetMemberIsRecursive(IMemberMapperData mapperData)
        {
            if (mapperData.TargetMember.IsRecursive)
            {
                return true;
            }

            var parentMapperData = mapperData.Parent;

            while (!parentMapperData.IsForStandaloneMapping)
            {
                if (parentMapperData.TargetMember.IsRecursive)
                {
                    // The target member we're mapping right now isn't recursive,
                    // but it's being mapped as part of the mapping of a recursive
                    // member. We therefore want to check if this member recurses
                    // later, and if so we'll map it by calling a mapping function:
                    var parentMember = parentMapperData.TargetMember.LeafMember;

                    if (TargetMemberRecursesWithin(parentMember, mapperData.TargetMember.LeafMember))
                    {
                        return true;
                    }
                }

                parentMapperData = parentMapperData.Parent;
            }

            return false;
        }

        private static bool TargetMemberRecursesWithin(Member parentMember, Member member)
        {
            var nonSimpleChildMembers = GlobalContext.Instance
                .MemberFinder
                .GetWriteableMembers(parentMember.Type)
                .Where(m => !m.IsSimple)
                .ToArray();

            return nonSimpleChildMembers.Contains(member) ||
                   nonSimpleChildMembers.Any(m => TargetMemberRecursesWithin(m, member));
        }

        private static Expression GetMapRecursionCallFor(
            IObjectMappingData childMappingData,
            Expression sourceValue,
            int dataSourceIndex,
            ObjectMapperData declaredTypeMapperData)
        {
            var childMapperData = childMappingData.MapperData;

            childMapperData.RegisterRequiredMapperFunc(childMappingData);

            var mapRecursionCall = declaredTypeMapperData.GetMapRecursionCall(
                sourceValue,
                childMapperData.TargetMember,
                dataSourceIndex);

            return mapRecursionCall;
        }

        public static Expression GetElementMapping(
            Expression sourceElementValue,
            Expression targetElementValue,
            IObjectMappingData enumerableMappingData)
        {
            var declaredTypeEnumerableMapperData = enumerableMappingData.MapperData;

            var elementMapperData = new ElementMapperData(
                sourceElementValue,
                targetElementValue,
                declaredTypeEnumerableMapperData);

            var elementMappingData = ObjectMappingDataFactory.ForElement(
                elementMapperData.SourceMember,
                elementMapperData.TargetMember,
                enumerableMappingData);

            if (elementMappingData.MapperKey.MappingTypes.RuntimeTypesNeeded)
            {
                return declaredTypeEnumerableMapperData.GetMapCall(sourceElementValue, targetElementValue);
            }

            return GetElementMapping(
                sourceElementValue,
                targetElementValue,
                declaredTypeEnumerableMapperData.EnumerablePopulationBuilder.Counter,
                elementMappingData,
                declaredTypeEnumerableMapperData);
        }

        private static Expression GetElementMapping(
            Expression sourceElementValue,
            Expression targetElementValue,
            Expression enumerableIndex,
            IObjectMappingData elementMappingData,
            IMemberMapperData declaredTypeMapperData)
        {
            var elementMapper = elementMappingData.Mapper;

            var inlineMappingBlock = GetInlineMappingBlock(
                elementMapper,
                elementMappingData.MapperData,
                MappingDataFactory.ForElementMethod,
                sourceElementValue,
                targetElementValue,
                enumerableIndex,
                declaredTypeMapperData.MappingDataObject);

            return inlineMappingBlock;
        }

        private static Expression GetInlineMappingBlock(
            IObjectMapper childMapper,
            IMemberMapperData childMapperData,
            MethodInfo createMethod,
            params Expression[] createMethodCallArguments)
        {
            var mappingExpression = MappingExpression.For(childMapper);

            if (!mappingExpression.IsSuccessful)
            {
                return childMapper.MappingExpression;
            }

            var inlineMappingDataVariable = childMapperData.MappingDataObject;

            var createInlineMappingDataCall = GetCreateMappingDataCall(
                createMethod,
                childMapperData,
                createMethodCallArguments);

            var inlineMappingDataAssignment = Expression
                .Assign(inlineMappingDataVariable, createInlineMappingDataCall);

            var updatedMappingExpression = mappingExpression.GetUpdatedMappingExpression(inlineMappingDataAssignment);

            return updatedMappingExpression;
        }

        private static Expression GetCreateMappingDataCall(
            MethodInfo createMethod,
            IBasicMapperData childMapperData,
            Expression[] createMethodCallArguments)
        {
            var inlineMappingTypes = new[] { childMapperData.SourceType, childMapperData.TargetType };

            return Expression.Call(
                createMethod.MakeGenericMethod(inlineMappingTypes),
                createMethodCallArguments);
        }

        #region Helper Class

        private class MappingExpression
        {
            private static readonly MappingExpression _unableToMap = new MappingExpression();

            private readonly TryExpression _mappingTryCatch;
            private readonly Func<BlockExpression, Expression> _finalMappingBlockFactory;

            private MappingExpression()
            {
            }

            private MappingExpression(
                TryExpression mappingTryCatch,
                Func<BlockExpression, Expression> finalMappingBlockFactory)
            {
                _mappingTryCatch = mappingTryCatch;
                _finalMappingBlockFactory = finalMappingBlockFactory;
                IsSuccessful = true;
            }

            #region Factory Method

            public static MappingExpression For(IObjectMapper mapper)
            {
                if (mapper.MappingExpression.NodeType == ExpressionType.Try)
                {
                    return new MappingExpression((TryExpression)mapper.MappingExpression, b => b);
                }

                var blockExpression = (BlockExpression)mapper.MappingExpression;

                var mappingTryCatch = blockExpression.Expressions.Last() as TryExpression;

                if (mappingTryCatch == null)
                {
                    return _unableToMap;
                }

                return new MappingExpression(
                    mappingTryCatch,
                    updatedTryCatch =>
                    {
                        var blockExpressions = blockExpression
                            .Expressions
                            .Take(blockExpression.Expressions.Count - 1)
                            .ToList();

                        blockExpressions.Add(updatedTryCatch);

                        return Expression.Block(blockExpression.Variables, blockExpressions);
                    });
            }

            #endregion

            public bool IsSuccessful { get; }

            public Expression GetUpdatedMappingExpression(BinaryExpression mappingDataAssignment)
            {
                var updatedTryCatch = _mappingTryCatch.Update(
                    Expression.Block(mappingDataAssignment, _mappingTryCatch.Body),
                    _mappingTryCatch.Handlers,
                    _mappingTryCatch.Finally,
                    _mappingTryCatch.Fault);

                var mappingDataVariable = (ParameterExpression)mappingDataAssignment.Left;
                var mappingBlock = Expression.Block(new[] { mappingDataVariable }, updatedTryCatch);
                var finalMappingBlock = _finalMappingBlockFactory.Invoke(mappingBlock);

                return finalMappingBlock;
            }
        }

        #endregion
    }
}