#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:35

#endregion


#region Usings

using System;

#endregion


namespace ServiceWire.Aspects
{
    public static class Interceptor
    {
        #region Methods


        #region Public Methods

        public static TTarget Intercept<TTarget>(TTarget target,CrossCuttingConcerns crossCuttingConcerns) where TTarget : class
        {
            return Intercept(0,target,crossCuttingConcerns);
        }

        public static TTarget Intercept<TTarget>(int id,TTarget target,CrossCuttingConcerns crossCuttingConcerns) where TTarget : class
        {
            if(!typeof(TTarget).IsInterface)
            {
                throw new ArgumentException("TTarget not an interface");
            }
            if(null==target)
            {
                throw new ArgumentNullException("target");
            }
            var interceptedTarget=ProxyFactory.CreateProxy<TTarget>(typeof(InterceptChannel),typeof(InterceptPoint),new InterceptPoint {Id=id,Target=target,Cut=crossCuttingConcerns});
            return interceptedTarget;
        }

        #endregion


        #endregion
    }
}