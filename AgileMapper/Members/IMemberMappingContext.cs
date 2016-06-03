﻿namespace AgileObjects.AgileMapper.Members
{
    using System.Linq.Expressions;
    using DataSources;
    using ObjectPopulation;

    internal interface IMemberMappingContext : IMappingData
    {
        MapperContext MapperContext { get; }

        MappingContext MappingContext { get; }

        new IObjectMappingContext Parent { get; }

        ParameterExpression Parameter { get; }

        IQualifiedMember SourceMember { get; }

        Expression SourceObject { get; }

        Expression ExistingObject { get; }

        Expression EnumerableIndex { get; }

        ParameterExpression InstanceVariable { get; }

        NestedAccessFinder NestedAccessFinder { get; }

        DataSourceSet GetDataSources();

        Expression WrapInTry(Expression expression);
    }
}