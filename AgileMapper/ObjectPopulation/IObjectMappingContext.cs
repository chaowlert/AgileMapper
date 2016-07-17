namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Linq.Expressions;
    using Members;

    internal interface IObjectMappingContext : IMemberMappingContext
    {
        GlobalContext GlobalContext { get; }

        new MapperContext MapperContext { get; }

        new IObjectMappingContext Parent { get; }

        TSource GetSource<TSource>();

        TTarget GetTarget<TTarget>();

        int? GetEnumerableIndex();

        void Set<TSourceElement, TTargetElement>(TSourceElement sourceElement, TTargetElement existingElement, int enumerableIndex);

        Type GetSourceMemberRuntimeType(IQualifiedMember sourceMember);

        MethodCallExpression TryGetCall { get; }

        MethodCallExpression ObjectRegistrationCall { get; }

        MethodCallExpression GetMapCall(Expression sourceObject, QualifiedMember objectMember, int dataSourceIndex);

        MethodCallExpression GetMapCall(Expression sourceElement, Expression existingElement);

        IObjectMappingContextFactoryBridge CreateChildMappingContextBridge<TDeclaredSource, TDeclaredMember>(
            TDeclaredSource source,
            TDeclaredMember targetMemberValue,
            string targetMemberName,
            int dataSourceIndex);

        IObjectMappingContextFactoryBridge CreateElementMappingContextBridge<TSourceElement, TTargetElement>(
            TSourceElement sourceElement,
            TTargetElement existingElement,
            int enumerableIndex);

        ITypedMemberMappingContext<TSource, TTarget> AsMemberContext<TSource, TTarget>();
    }
}