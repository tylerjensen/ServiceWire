using System;

namespace ServiceWire.Aspects
{
    public static class Interceptor
    {
        public static TTarget Intercept<TTarget>(TTarget target, CrossCuttingConcerns crossCuttingConcerns, ISerializer serializer = null, ICompressor compressor = null) where TTarget : class
        {
            return Intercept<TTarget>(0, target, crossCuttingConcerns, serializer, compressor);
        }

        public static TTarget Intercept<TTarget>(int id, TTarget target, CrossCuttingConcerns crossCuttingConcerns, ISerializer serializer = null, ICompressor compressor = null) where TTarget : class
        {
            if (!typeof(TTarget).IsInterface) throw new ArgumentException("TTarget not an interface");
            if (null == target) throw new ArgumentNullException("target");
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            TTarget interceptedTarget = ProxyFactory.CreateProxy<TTarget>(typeof(InterceptChannel),
            typeof(InterceptPoint),
            new InterceptPoint
            {
                Id = id,
                Target = target,
                Cut = crossCuttingConcerns
            },
            serializer,
            compressor);
            return interceptedTarget;
        }
    }
}
