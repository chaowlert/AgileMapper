namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Members;

    internal class ObjectMappingData<TSource, TTarget> :
        MappingInstanceData<TSource, TTarget>,
        IObjectMappingData,
        IObjectMappingData<TSource, TTarget>,
        IObjectCreationMappingData<TSource, TTarget, TTarget>
    {
        private readonly IObjectMappingData _parent;
        private readonly Dictionary<object, List<object>> _mappedObjectsBySource;
        private ObjectMapper<TSource, TTarget> _mapper;
        private ObjectMapperData _mapperData;

        public ObjectMappingData(
            TSource source,
            TTarget target,
            int? enumerableIndex,
            ObjectMapperKeyBase mapperKey,
            IMappingContext mappingContext,
            IObjectMappingData parent)
            : this(
                  source,
                  target,
                  enumerableIndex,
                  mapperKey,
                  mappingContext,
                  null,
                  parent)
        {
        }

        private ObjectMappingData(
            TSource source,
            TTarget target,
            int? enumerableIndex,
            ObjectMapperKeyBase mapperKey,
            IMappingContext mappingContext,
            IObjectMappingData declaredTypeMappingData,
            IObjectMappingData parent)
            : base(source, target, enumerableIndex, parent)
        {
            MapperKey = mapperKey;
            MappingContext = mappingContext;
            DeclaredTypeMappingData = declaredTypeMappingData;

            if (parent != null)
            {
                _parent = parent;
                return;
            }

            IsRoot = true;

            if (IsPartOfDerivedTypeMapping)
            {
                return;
            }

            _mapper = MapperContext.ObjectMapperFactory.GetOrCreateRoot(this);

            if (MapperData.MappedObjectCachingNeeded)
            {
                _mappedObjectsBySource = new Dictionary<object, List<object>>(13);
            }
        }

        public IMappingContext MappingContext { get; }

        public MapperContext MapperContext => MappingContext.MapperContext;

        public ObjectMapperKeyBase MapperKey { get; }

        IObjectMapper IObjectMappingData.Mapper
        {
            get { return Mapper; }
            set { _mapper = (ObjectMapper<TSource, TTarget>)value; }
        }

        private ObjectMapper<TSource, TTarget> Mapper
            => _mapper ?? (_mapper = MapperContext.ObjectMapperFactory.Create(this));

        public TTarget CreatedObject { get; set; }

        #region IObjectMappingData Members

        public bool IsRoot { get; }

        IObjectMappingData IObjectMappingData.Parent => _parent;

        IObjectMappingDataUntyped IObjectMappingData<TSource, TTarget>.Parent => _parent;

        public bool IsPartOfDerivedTypeMapping => DeclaredTypeMappingData != null;

        public IObjectMappingData DeclaredTypeMappingData { get; }

        public ObjectMapperData MapperData
            => _mapperData ?? (_mapperData = _mapper?.MapperData ?? ObjectMapperData.For<TSource, TTarget>(this));

        private ChildMemberMappingData<TSource, TTarget> _childMappingData;

        IChildMemberMappingData IObjectMappingData.GetChildMappingData(IMemberMapperData childMapperData)
        {
            if (_childMappingData == null)
            {
                _childMappingData = new ChildMemberMappingData<TSource, TTarget>(this);
            }

            _childMappingData.MapperData = childMapperData;

            return _childMappingData;
        }

        #endregion

        #region Map Methods

        public object MapStart() => _mapper.Map(this);

        public TDeclaredTarget Map<TDeclaredSource, TDeclaredTarget>(
            TDeclaredSource sourceValue,
            TDeclaredTarget targetValue,
            string targetMemberName,
            int dataSourceIndex)
        {
            return (TDeclaredTarget)Mapper.MapChild(
                sourceValue,
                targetValue,
                GetEnumerableIndex(),
                targetMemberName,
                dataSourceIndex,
                this);
        }

        public TTargetElement Map<TSourceElement, TTargetElement>(
            TSourceElement sourceElement,
            TTargetElement targetElement,
            int enumerableIndex)
        {
            return (TTargetElement)Mapper.MapElement(
                sourceElement,
                targetElement,
                enumerableIndex,
                this);
        }

        public TDeclaredTarget MapRecursion<TDeclaredSource, TDeclaredTarget>(
            TDeclaredSource sourceValue,
            TDeclaredTarget targetValue,
            string targetMemberName,
            int dataSourceIndex)
        {
            if (IsRoot || MapperKey.MappingTypes.RuntimeTypesNeeded)
            {
                return (TDeclaredTarget)Mapper.MapRecursion(
                    sourceValue,
                    targetValue,
                    GetEnumerableIndex(),
                    targetMemberName,
                    dataSourceIndex,
                    this);
            }

            return _parent.MapRecursion(
                sourceValue,
                targetValue,
                targetMemberName,
                dataSourceIndex);
        }

        #endregion

        public bool TryGet<TKey, TComplex>(TKey key, out TComplex complexType)
            where TComplex : class
        {
            if (!IsRoot)
            {
                return _parent.TryGet(key, out complexType);
            }

            List<object> mappedTargets;

            if (_mappedObjectsBySource.TryGetValue(key, out mappedTargets))
            {
                complexType = (TComplex)mappedTargets.FirstOrDefault(t => t is TComplex);
                return complexType != null;
            }

            complexType = default(TComplex);
            return false;
        }

        public void Register<TKey, TComplex>(TKey key, TComplex complexType)
        {
            if (!IsRoot)
            {
                _parent.Register(key, complexType);
                return;
            }

            List<object> mappedTargets;

            if (_mappedObjectsBySource.TryGetValue(key, out mappedTargets))
            {
                mappedTargets.Add(complexType);
                return;
            }

            _mappedObjectsBySource[key] = new List<object> { complexType };
        }

        public IObjectMappingData WithTypes(Type newSourceType, Type newTargetType)
        {
            var typesKey = new SourceAndTargetTypesKey(newSourceType, newTargetType);

            var typedWithTypesCaller = GlobalContext.Instance.Cache.GetOrAdd(typesKey, k =>
            {
                var mappingDataParameter = Parameters.Create<IObjectMappingData<TSource, TTarget>>("mappingData");
                var withTypesCall = mappingDataParameter.GetAsCall(k.SourceType, k.TargetType);

                var withTypesLambda = Expression
                    .Lambda<Func<IObjectMappingData<TSource, TTarget>, IObjectMappingDataUntyped>>(
                        withTypesCall,
                        mappingDataParameter);

                return withTypesLambda.Compile();
            });

            return (IObjectMappingData)typedWithTypesCaller.Invoke(this);
        }

        public IObjectMappingData<TNewSource, TNewTarget> As<TNewSource, TNewTarget>()
            where TNewSource : class
            where TNewTarget : class
        {
            return new ObjectMappingData<TNewSource, TNewTarget>(
                Source as TNewSource,
                Target as TNewTarget,
                GetEnumerableIndex(),
                MapperKey.WithTypes<TNewSource, TNewTarget>(),
                MappingContext,
                this,
                _parent);
        }
    }
}