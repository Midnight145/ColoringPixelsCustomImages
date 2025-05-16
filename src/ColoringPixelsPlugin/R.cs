using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace ColoringPixelsMod {
    public class R {
        private object obj;
        private Type type;
        
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
            if (field == null) throw new MissingFieldException($"{type.Name}.{fieldName}");
            return field.GetValue(obj);
        }
        
        public void SetField(string fieldName, object value) {
            AccessTools.Field(type, fieldName).SetValue(obj, value);
        }
        
        public object CallMethod(string methodName, params object[] parameters) {
            var method = AccessTools.Method(type, methodName, parameters.Select(p => p?.GetType()).ToArray());
            if (method == null) throw new MissingMethodException($"{type.Name}.{methodName}");
            return method.Invoke(obj, parameters);
        }

        public static R of(Type type) {
            return new R(type);
        }
        
        public static R of(object obj) {
            return new R(obj);
        }
    }
}
