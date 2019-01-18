using System;

namespace ServiceWire.Aspects
{
    public static class Interceptor
    {
        public static TTarget Intercept<TTarget>(TTarget target, CrossCuttingConcerns crossCuttingConcerns, ISerializer serializer = null) where TTarget : class
        {
            return Intercept<TTarget>(0, target, crossCuttingConcerns, serializer);
        }

        public static TTarget Intercept<TTarget>(int id, TTarget target, CrossCuttingConcerns crossCuttingConcerns, ISerializer serializer = null) where TTarget : class
        {
            if (!typeof(TTarget).IsInterface) throw new ArgumentException("TTarget not an interface");
            if (null == target) throw new ArgumentNullException("target");
            if (null == serializer) serializer = new DefaultSerializer();
            TTarget interceptedTarget = ProxyFactory.CreateProxy<TTarget>(typeof(InterceptChannel), 
            typeof(InterceptPoint), 
            new InterceptPoint 
            { 
                Id = id,
                Target = target,
                Cut = crossCuttingConcerns 
            },
            serializer);
            return interceptedTarget;
        }
    }
}
