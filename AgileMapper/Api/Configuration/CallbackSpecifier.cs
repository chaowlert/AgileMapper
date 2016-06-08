﻿namespace AgileObjects.AgileMapper.Api.Configuration
{
    using System;
    using System.Linq.Expressions;
    using Members;
    using ObjectPopulation;

    internal class CallbackSpecifier<TSource, TTarget> :
        CallbackSpecifierBase,
        IConditionalCallbackSpecifier<TSource, TTarget>
    {
        private readonly QualifiedMember _targetMember;

        public CallbackSpecifier(
            MapperContext mapperContext,
            CallbackPosition callbackPosition,
            QualifiedMember targetMember)
            : this(
                  new MappingConfigInfo(mapperContext).ForAllRuleSets().ForAllSourceTypes(),
                  callbackPosition,
                  targetMember)
        {
        }

        public CallbackSpecifier(
            MappingConfigInfo configInfo,
            CallbackPosition callbackPosition,
            QualifiedMember targetMember)
            : base(callbackPosition, configInfo)
        {
            _targetMember = targetMember;
        }

        public ICallbackSpecifier<TSource, TTarget> If(
            Expression<Func<ITypedMemberMappingContext<TSource, TTarget>, bool>> condition)
            => SetCondition(condition);

        public ICallbackSpecifier<TSource, TTarget> If(Expression<Func<TSource, TTarget, bool>> condition)
            => SetCondition(condition);

        public ICallbackSpecifier<TSource, TTarget> If(Expression<Func<TSource, TTarget, int?, bool>> condition)
            => SetCondition(condition);

        private CallbackSpecifier<TSource, TTarget> SetCondition(LambdaExpression conditionLambda)
        {
            ConfigInfo.AddCondition(conditionLambda);
            return this;
        }

        public void Call(Action<ITypedMemberMappingContext<TSource, TTarget>> callback) => CreateCallbackFactory(callback);

        public void Call(Action<TSource, TTarget> callback) => CreateCallbackFactory(callback);

        public void Call(Action<TSource, TTarget, int?> callback) => CreateCallbackFactory(callback);

        private void CreateCallbackFactory<TAction>(TAction callback)
        {
            var callbackLambda = ConfiguredLambdaInfo.ForAction(callback, typeof(TSource), typeof(TTarget));

            var creationCallbackFactory = new MappingCallbackFactory(
                ConfigInfo.ForTargetType<TTarget>(),
                CallbackPosition,
                callbackLambda,
                _targetMember);

            ConfigInfo.MapperContext.UserConfigurations.Add(creationCallbackFactory);
        }
    }
}