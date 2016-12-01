﻿namespace AgileObjects.AgileMapper.Extensions
{
    using System.Reflection;
    using NetStandardPolyfills;

    internal static class ReflectionExtensions
    {
        public static readonly bool ReflectionNotPermitted;

        static ReflectionExtensions()
        {
            try
            {
                typeof(TrustTester)
                    .GetNonPublicStaticMethod("IsReflectionPermitted")
                    .Invoke(null, null);
            }
            catch
            {
                ReflectionNotPermitted = true;
            }
        }

        public static bool IsReadable(this PropertyInfo property)
        {
            return property.GetGetMethod(nonPublic: false) != null;
        }

        public static bool IsWriteable(this PropertyInfo property)
        {
            return property.GetSetMethod(nonPublic: false) != null;
        }
    }

    internal class TrustTester
    {
        // ReSharper disable once UnusedMember.Local
        private static void IsReflectionPermitted() { }
    }
}