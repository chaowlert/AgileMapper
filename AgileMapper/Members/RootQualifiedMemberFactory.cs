namespace AgileObjects.AgileMapper.Members
{
    using Caching;

    internal class RootQualifiedMemberFactory
    {
        private readonly MapperContext _mapperContext;
        private readonly ICache<QualifiedMemberKey, IQualifiedMember> _memberCache;

        public RootQualifiedMemberFactory(MapperContext mapperContext)
        {
            _mapperContext = mapperContext;
            _memberCache = mapperContext.Cache.CreateScoped<QualifiedMemberKey, IQualifiedMember>();
        }

        public IQualifiedMember RootSource<TSource>()
        {
            var memberKey = QualifiedMemberKey.ForSource<TSource>();

            var rootMember = _memberCache.GetOrAdd(
                memberKey,
                k => QualifiedMember.From(Member.RootSource<TSource>(), _mapperContext));

            return rootMember;
        }

        public QualifiedMember RootTarget<TTarget>()
        {
            var memberKey = QualifiedMemberKey.ForTarget<TTarget>();

            var rootMember = _memberCache.GetOrAdd(
                memberKey,
                k => QualifiedMember.From(Member.RootTarget<TTarget>(), _mapperContext));

            return (QualifiedMember)rootMember;
        }

        private class QualifiedMemberKey
        {
            public static QualifiedMemberKey ForSource<TSource>() => SourceKey<TSource>.Instance;

            public static QualifiedMemberKey ForTarget<TTarget>() => TargetKey<TTarget>.Instance;

            // ReSharper disable once UnusedTypeParameter
            private static class SourceKey<T>
            {
                // ReSharper disable once StaticMemberInGenericType
                public static readonly QualifiedMemberKey Instance = new QualifiedMemberKey();
            }

            // ReSharper disable once UnusedTypeParameter
            private static class TargetKey<T>
            {
                // ReSharper disable once StaticMemberInGenericType
                public static readonly QualifiedMemberKey Instance = new QualifiedMemberKey();
            }
        }
    }
}