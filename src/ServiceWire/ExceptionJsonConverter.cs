using System;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceWire
{
    /// <summary>
    /// System.Text.Json cannot serialize thrown exceptions (TargetSite is a MethodBase,
    /// which it refuses), which used to kill the connection whenever a service method
    /// threw. This converter writes a stable JSON shape using the exception's own
    /// property names (so pre-7.0 receivers parse it without error) and reconstructs
    /// the original exception type, message, HResult, inner chain, and server stack
    /// trace on receive.
    /// </summary>
    internal sealed class ExceptionJsonConverter : JsonConverter<Exception>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("ClassName", value.GetType().ToConfigName());
            writer.WriteString("Message", value.Message);
            writer.WriteNumber("HResult", value.HResult);
            writer.WriteString("Source", value.Source);
            writer.WriteString("StackTrace", value.StackTrace);
            if (null != value.InnerException)
            {
                writer.WritePropertyName("InnerException");
                Write(writer, value.InnerException, options);
            }
            writer.WriteEndObject();
        }

        public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                return ReadException(doc.RootElement, typeToConvert);
            }
        }

        private static Exception ReadException(JsonElement element, Type declaredType)
        {
            string className = GetString(element, "ClassName");
            string message = GetString(element, "Message");
            Exception inner = null;
            if (element.TryGetProperty("InnerException", out var innerElement) && innerElement.ValueKind == JsonValueKind.Object)
            {
                inner = ReadException(innerElement, typeof(Exception));
            }

            var type = (null != className ? className.ToType() : null) ?? declaredType;
            if (!typeof(Exception).IsAssignableFrom(type)) type = typeof(Exception);

            var exception = Construct(type, message, inner);

            if (element.TryGetProperty("HResult", out var hresultElement) && hresultElement.ValueKind == JsonValueKind.Number)
            {
                TrySetHResult(exception, hresultElement.GetInt32());
            }
            var stackTrace = GetString(element, "StackTrace");
            if (null != stackTrace) TrySetRemoteStackTrace(exception, stackTrace);
            return exception;
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }

        /// <summary>
        /// Rebuilds the exception, preferring the most faithful construction the remote
        /// type supports. Each step is a best effort over an arbitrary user-defined type
        /// whose constructors and internals we do not control, so a failure is not an
        /// error: it just means the next, less faithful strategy is used. The final
        /// fallback always succeeds, so this method never throws.
        /// </summary>
        private static Exception Construct(Type type, string message, Exception inner)
        {
            //1. the conventional (message, innerException) constructor
            try
            {
                return (Exception)Activator.CreateInstance(type, message, inner);
            }
            catch (Exception)
            {
                //the type does not offer that constructor, or it rejected these arguments
            }

            //2. the (message) constructor, when there is no inner exception to carry
            try
            {
                if (null == inner) return (Exception)Activator.CreateInstance(type, message);
            }
            catch (Exception)
            {
                //likewise: fall through to constructing the type without a constructor
            }

            //3. bypass constructors entirely and set the message field directly, which
            //   preserves the caller's ability to catch the original exception type
            try
            {
#if NET8_0_OR_GREATER
                var exception = (Exception)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
#else
                var exception = (Exception)FormatterServices.GetUninitializedObject(type);
#endif
                var messageField = typeof(Exception).GetField("_message",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (null != messageField) messageField.SetValue(exception, message);
                return exception;
            }
            catch (Exception)
            {
                //uninitialized construction is refused for some types, and the private
                //field layout is not contractual; the plain Exception below always works
            }

            //4. the type could not be rebuilt: the caller still gets the message and chain
            return new Exception(message, inner);
        }

        private static void TrySetHResult(Exception exception, int hresult)
        {
            try
            {
                var prop = typeof(Exception).GetProperty("HResult");
                var setter = prop?.GetSetMethod(true);
                setter?.Invoke(exception, new object[] { hresult });
            }
            catch (Exception)
            {
                //HResult is a diagnostic detail, not part of the contract. Its setter is
                //non-public and may be absent or refused on a future runtime; losing it
                //must not cost the caller the exception itself.
            }
        }

        private static void TrySetRemoteStackTrace(Exception exception, string stackTrace)
        {
            try
            {
                var field = typeof(Exception).GetField("_remoteStackTraceString",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(exception, stackTrace + Environment.NewLine);
            }
            catch (Exception)
            {
                //the server stack trace is a diagnostic nicety carried through a private
                //runtime field; if the field is gone the exception is still correct
            }
        }
    }
}
