﻿namespace AgileObjects.AgileMapper.PerformanceTester.ConcreteMappers.Mapster
{
    using AbstractMappers;
    using global::Mapster;
    using TestClasses;

    internal class MapsterFlatteningMapper : FlatteningMapperBase
    {
        public override void Initialise()
        {
            TypeAdapterConfig<ModelObject, ModelDto>.NewConfig()
                .Compile();
        }

        protected override ModelDto Flatten(ModelObject model)
        {
            return model.Adapt<ModelObject, ModelDto>();
        }
    }
}