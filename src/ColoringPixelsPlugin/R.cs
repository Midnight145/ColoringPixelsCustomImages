using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ColoringPixelsMod {
    public class R {
        private readonly object obj;
        private readonly Type type;
        
        public R(Type type) {
            this.type = type;
            this.obj = null;
        }
        
        public R(object obj) {
            this.obj = obj;
            this.type = obj.GetType();
        }
        
        public object GetField(string fieldName) {
            FieldInfo field = AccessTools.Field(type, fieldName);
            return field.GetValue(obj);
        }
        
        public FieldInfo GetFieldInfo(string fieldName) {
            FieldInfo field = AccessTools.Field(type, fieldName);
            if (field == null) throw new MissingFieldException($"{type.Name}.{fieldName}");
            return field;
        }
        
        public void SetField(string fieldName, object value) {
            AccessTools.Field(type, fieldName).SetValue(obj, value);
        }
        
        public object CallMethod(string methodName, params object[] parameters) {
            MethodInfo method = GetMethodInfo(methodName, parameters.Select(p => p.GetType()).ToArray());
            return method.Invoke(obj, parameters);
        }

        public MethodInfo GetMethodInfo(string methodName, params Type[] parameterTypes) {
            MethodInfo method = AccessTools.Method(type, methodName, parameterTypes);
            if (method == null) throw new MissingMethodException($"{type.Name}.{methodName}");
            return method;
        }
        
        public static R of(Type type) {
            return new R(type);
        }
        
        public static R of(object obj) {
            return new R(obj);
        }
    }
}
