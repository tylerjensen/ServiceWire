using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceWire
{
    /// <summary>
    /// Compiles reflection members into delegates so the per-call dispatch path
    /// avoids MethodInfo.Invoke and PropertyInfo.GetValue. Compiled delegates throw
    /// the raw exception (no TargetInvocationException wrapper); callers that unwrap
    /// conditionally see identical client-visible behavior.
    /// </summary>
    internal static class MethodInvokerCompiler
    {
        /// <summary>
        /// Returns a compiled invoker for the method, or null when the method cannot
        /// be compiled (byref parameters, or an Expression limitation on the current
        /// runtime) and the caller must fall back to MethodInfo.Invoke.
        /// </summary>
        public static Func<object, object[], object> TryCompile(MethodInfo method)
        {
            var parameters = method.GetParameters();
            //byref write-back through an object[] needs block/local plumbing that is
            //not worth the risk; those methods keep the reflection path
            foreach (var p in parameters)
                if (p.ParameterType.IsByRef) return null;

            try
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var argsParam = Expression.Parameter(typeof(object[]), "args");
                var argExprs = new Expression[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    argExprs[i] = Expression.Convert(
                        Expression.ArrayIndex(argsParam, Expression.Constant(i)),
                        parameters[i].ParameterType);
                }
                var call = Expression.Call(
                    Expression.Convert(instanceParam, method.DeclaringType),
                    method, argExprs);
                Expression body = method.ReturnType == typeof(void)
                    ? (Expression)Expression.Block(call, Expression.Constant(null, typeof(object)))
                    : Expression.Convert(call, typeof(object));
                return Expression.Lambda<Func<object, object[], object>>(body, instanceParam, argsParam).Compile();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a compiled getter for the Result property of the concrete task type,
        /// or null when the type has no Result property (plain Task).
        /// </summary>
        public static Func<Task, object> TryCompileTaskResultGetter(Type taskType)
        {
            var prop = taskType.GetProperty("Result");
            if (null == prop) return null;
            try
            {
                var taskParam = Expression.Parameter(typeof(Task), "task");
                var body = Expression.Convert(
                    Expression.Property(Expression.Convert(taskParam, taskType), prop),
                    typeof(object));
                return Expression.Lambda<Func<Task, object>>(body, taskParam).Compile();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a compiled converter that wraps a boxed value in Task.FromResult&lt;T&gt;
        /// for the given Task&lt;T&gt; return type.
        /// </summary>
        public static Func<object, object> CompileTaskFromResult(Type taskOfTType)
        {
            var resultType = taskOfTType.GenericTypeArguments[0];
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult)).MakeGenericMethod(resultType);
            var valueParam = Expression.Parameter(typeof(object), "v");
            var call = Expression.Call(fromResult, Expression.Convert(valueParam, resultType));
            return Expression.Lambda<Func<object, object>>(Expression.Convert(call, typeof(object)), valueParam).Compile();
        }
    }
}
