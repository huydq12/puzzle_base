using System;
using System.Collections.Generic;
using System.Reflection;

namespace AZUR
{
    internal static class AzurReflection
    {
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();

        public static Type FindType(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            if (TypeCache.TryGetValue(fullName, out var cached))
            {
                return cached;
            }

            var direct = Type.GetType(fullName);
            if (direct != null)
            {
                TypeCache[fullName] = direct;
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var index = 0; index < assemblies.Length; index++)
            {
                var type = assemblies[index].GetType(fullName);
                if (type != null)
                {
                    TypeCache[fullName] = type;
                    return type;
                }
            }

            TypeCache[fullName] = null;
            return null;
        }

        public static bool HasType(string fullName)
        {
            return FindType(fullName) != null;
        }

        public static object CreateInstance(string fullName, params object[] args)
        {
            var type = FindType(fullName);
            return type == null ? null : Activator.CreateInstance(type, args);
        }

        public static object InvokeStatic(string fullName, string methodName, params object[] args)
        {
            var type = FindType(fullName);
            return type == null ? null : InvokeMethod(type, null, methodName, args);
        }

        public static object InvokeInstance(object instance, string methodName, params object[] args)
        {
            return instance == null ? null : InvokeMethod(instance.GetType(), instance, methodName, args);
        }

        public static object GetStaticProperty(string fullName, string propertyName)
        {
            var type = FindType(fullName);
            return type == null ? null : GetProperty(type, null, propertyName);
        }

        public static object GetInstanceProperty(object instance, string propertyName)
        {
            return instance == null ? null : GetProperty(instance.GetType(), instance, propertyName);
        }

        public static bool SetInstanceProperty(object instance, string propertyName, object value)
        {
            if (instance == null)
            {
                return false;
            }

            var property = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null || !property.CanWrite)
            {
                return false;
            }

            property.SetValue(instance, ConvertValue(value, property.PropertyType));
            return true;
        }

        public static object GetStaticField(string fullName, string fieldName)
        {
            var type = FindType(fullName);
            if (type == null)
            {
                return null;
            }

            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.GetValue(null);
        }

        public static object ParseEnum(string fullName, string value)
        {
            var type = FindType(fullName);
            return type == null ? null : Enum.Parse(type, value);
        }

        private static object InvokeMethod(Type type, object instance, string methodName, object[] args)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            for (var index = 0; index < methods.Length; index++)
            {
                var method = methods[index];
                if (method.Name != methodName)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != (args?.Length ?? 0))
                {
                    continue;
                }

                var converted = new object[parameters.Length];
                var matches = true;
                for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    if (!TryConvertValue(args[parameterIndex], parameters[parameterIndex].ParameterType, out converted[parameterIndex]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return method.Invoke(instance, converted);
                }
            }

            return null;
        }

        private static object GetProperty(Type type, object instance, string propertyName)
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            return property?.GetValue(instance);
        }

        private static bool TryConvertValue(object value, Type targetType, out object converted)
        {
            if (targetType == typeof(object))
            {
                converted = value;
                return true;
            }

            if (value == null)
            {
                converted = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            }

            var valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
            {
                converted = value;
                return true;
            }

            try
            {
                if (targetType.IsEnum && value is string stringValue)
                {
                    converted = Enum.Parse(targetType, stringValue);
                    return true;
                }

                converted = Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                converted = null;
                return false;
            }
        }

        private static object ConvertValue(object value, Type targetType)
        {
            return TryConvertValue(value, targetType, out var converted) ? converted : value;
        }
    }
}
