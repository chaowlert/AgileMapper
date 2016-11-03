﻿namespace AgileObjects.AgileMapper.Members
{
    using System;
    using ObjectPopulation;

    internal interface IMemberMappingData
    {
        MappingRuleSet RuleSet { get; }

        IObjectMappingData Parent { get; }

        IMemberMapperData MapperData { get; }

        Type GetSourceMemberRuntimeType(IQualifiedMember sourceMember);
    }
}