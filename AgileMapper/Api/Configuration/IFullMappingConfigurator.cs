﻿namespace AgileObjects.AgileMapper.Api.Configuration
{
    using System;
    using Members;

    /// <summary>
    /// Provides options for configuring mappings from and to a given source and target type.
    /// </summary>
    /// <typeparam name="TSource">The source type to which the configuration should apply.</typeparam>
    /// <typeparam name="TTarget">The target type to which the configuration should apply.</typeparam>
    public interface IFullMappingConfigurator<TSource, TTarget> : IConditionalMappingConfigurator<TSource, TTarget>
    {
        /// <summary>
        /// Configure this mapper to perform an action before a different specified action.
        /// </summary>
        PreEventMappingConfigStartingPoint<TSource, TTarget> Before { get; }

        /// <summary>
        /// Configure this mapper to perform an action after a different specified action.
        /// </summary>
        PostEventMappingConfigStartingPoint<TSource, TTarget> After { get; }

        /// <summary>
        /// Swallow exceptions thrown during a mapping from and to the source and target types being configured. 
        /// Object mappings which encounter an Exception will return null.
        /// </summary>
        void SwallowAllExceptions();

        /// <summary>
        /// Pass Exceptions thrown during a mapping from and to the source and target types being configured to 
        /// the given <paramref name="callback"/> instead of throwing them.
        /// </summary>
        /// <param name="callback">
        /// The callback to which to pass thrown Exception information. If the thrown exception should not be 
        /// swallowed, it should be rethrown inside the callback.
        /// </param>
        void PassExceptionsTo(Action<IMappingExceptionData<TSource, TTarget>> callback);

        /// <summary>
        /// Configure this mapper to keep track of objects during a mapping in order to short-circuit 
        /// circular relationships and ensure a 1-to-1 relationship between source and mapped objects.
        /// </summary>
        /// <returns>
        /// A MappingConfigContinuation to enable further configuration of mappings from and to the source and 
        /// target type being configured.
        /// </returns>
        MappingConfigContinuation<TSource, TTarget> TrackMappedObjects();

        /// <summary>
        /// Configure a derived target type to which to map instances of the given derived source type.
        /// </summary>
        /// <typeparam name="TDerivedSource">
        /// The derived source type for which to configure a matching derived target type.
        /// </typeparam>
        /// <returns>A DerivedPairTargetTypeSpecifier with which to specify the matching derived target type.</returns>
        DerivedPairTargetTypeSpecifier<TSource, TDerivedSource, TTarget> Map<TDerivedSource>()
            where TDerivedSource : TSource;
    }
}