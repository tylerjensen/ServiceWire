using System;

namespace ServiceWire.Aspects
{
    public static class Interceptor
    {
        public static TTarget Intercept<TTarget>(TTarget target, CrossCuttingConcerns crossCuttingConcerns, ISerializer serializer = null, ICompressor compressor = null, 
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000) where TTarget : class
        {
            return Intercept<TTarget>(0, target, crossCuttingConcerns, serializer, compressor, identity, identityKey, log, stats, invokeTimeoutMs);
        }

        public static TTarget Intercept<TTarget>(int id, TTarget target, CrossCuttingConcerns crossCuttingConcerns, ISerializer serializer = null, ICompressor compressor = null,
            string identity = null, string identityKey = null, ILog log = null, IStats stats = null, int invokeTimeoutMs = 90000) where TTarget : class
        {
            if (!typeof(TTarget).IsInterface) throw new ArgumentException("TTarget not an interface");
            if (null == target) throw new ArgumentNullException("target");
            if (null == serializer) serializer = new DefaultSerializer();
            if (null == compressor) compressor = new DefaultCompressor();
            if (null == log) log = new NullLogger();
            if (null == stats) stats = new NullStats();
            TTarget interceptedTarget = ProxyFactory.CreateProxy<TTarget>(typeof(InterceptChannel),
            typeof(InterceptPoint),
            new InterceptPoint
            {
                Id = id,
                Target = target,
                Cut = crossCuttingConcerns
            },
            serializer,
            compressor,
            identity,
            identityKey,
            log,
            stats,
            invokeTimeoutMs);
            return interceptedTarget;
        }
    }
}
