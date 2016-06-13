﻿namespace AgileObjects.AgileMapper.Api
{
    public class TargetTypeSelector<TSource>
    {
        private readonly TSource _source;
        private readonly MapperContext _mapperContext;

        internal TargetTypeSelector(TSource source, MapperContext mapperContext)
        {
            _source = source;
            _mapperContext = mapperContext;
        }

        public TResult ToANew<TResult>() where TResult : class
            => PerformMapping(_mapperContext.RuleSets.CreateNew, default(TResult));

        public TTarget OnTo<TTarget>(TTarget existing) where TTarget : class
            => PerformMapping(_mapperContext.RuleSets.Merge, existing);

        public TTarget Over<TTarget>(TTarget existing) where TTarget : class
            => PerformMapping(_mapperContext.RuleSets.Overwrite, existing);

        private TTarget PerformMapping<TTarget>(MappingRuleSet ruleSet, TTarget existing)
        {
            using (var mappingContext = new MappingContext(ruleSet, _mapperContext))
            {
                return mappingContext.MapStart(_source, existing);
            }
        }
    }
}