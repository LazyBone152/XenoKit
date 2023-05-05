using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xv2CoreLib.BAC;
using Xv2CoreLib.Resource.UndoRedo;
using YAXLib;

namespace XenoKit.Helper.Find
{
    public static class Find
    {
        public static List<IUndoRedo> ReplaceBacValue(IList<BAC_Entry> entries, Type bacType, string valueName, object value, object replaceWith, out int numReplaced)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            int num = 0;

            foreach (var entry in entries)
            {
                foreach (var type in entry.IBacTypes)
                {
                    if (type.GetType() == bacType)
                    {
                        var values = ParseAllProps(type);
                        var found = values.FirstOrDefault(x => x.valueName == valueName);

                        if (found.valueName == valueName && found.value.Equals(value))
                        {
                            ReplaceValue(found.parent, found.valueName, replaceWith, undos);
                            num++;
                        }
                    }
                }
            }

            numReplaced = num;

            return undos;
        }

        /// <summary>
        /// Find the specified bac entry
        /// </summary>
        public static void FindBacValue(IList<BAC_Entry> entries, Type bacType, string valueName, object value, object prevFoundType, bool isNot, out BAC_Entry bacEntryRet, out object bacTypeRet)
        {
            bool matchValue = value != null;
            bool hadPrevType = prevFoundType != null ? true : false;

            foreach(var entry in entries)
            {
                foreach(var type in entry.IBacTypes)
                {
                    if(type.GetType() == bacType && prevFoundType == null)
                    {
                        var values = ParseAllProps(type);
                        var found = values.FirstOrDefault(x => x.valueName == valueName);

                        //Look for a specific value
                        if(((found.valueName == valueName && !matchValue) || (found.valueName == valueName && found.value.Equals(value)) || string.IsNullOrWhiteSpace(valueName)) && !isNot)
                        {
                            bacEntryRet = entry;
                            bacTypeRet = type;
                            return;
                        }

                        //NOT mode. Return first instance when its not the specified value.
                        if(found.ValueName == valueName && !found.value.Equals(value) && isNot)
                        {
                            bacEntryRet = entry;
                            bacTypeRet = type;
                            return;
                        }
                    }

                    if (prevFoundType != null && type == prevFoundType)
                        prevFoundType = null;
                }
            }

            //Nothing was found, but there was a previous type found. Restart the search.
            if (hadPrevType)
            {
                FindBacValue(entries, bacType, valueName, value, null, isNot, out bacEntryRet, out bacTypeRet);
                return;
            }

            bacEntryRet = null;
            bacTypeRet = null;
        }


        //Helpers:
        private static List<Value> ParseAllProps(object obj)
        {
            return ParseAllProps(obj.GetType(), obj);
        }

        public static List<Value> ParseAllProps(Type type, object obj = null)
        {
            PropertyInfo[] props = type.GetProperties();
            List<Value> values = new List<Value>();

            foreach (var childProp in props)
            {
                if (childProp.GetSetMethod() != null && childProp.GetGetMethod() != null)
                {
                    var yaxDontSerializeAttr = (YAXDontSerializeAttribute[])childProp.GetCustomAttributes(typeof(YAXDontSerializeAttribute), false);

                    if (yaxDontSerializeAttr.Length == 0)
                    {
                        if (obj != null)
                        {
                            values.Add(new Value(childProp.PropertyType, childProp.Name, childProp.GetValue(obj), obj));
                        }
                        else
                        {
                            values.Add(new Value(childProp.PropertyType, childProp.Name, null, obj));
                        }
                    }
                }
            }

            return values;
        }

        private static void ReplaceValue(object obj, string valueName, object newValue, List<IUndoRedo> undos = null)
        {
            if (undos != null)
                undos.Add(new UndoablePropertyGeneric(valueName, obj, obj.GetType().GetProperty(valueName).GetValue(obj), newValue));

            obj.GetType().GetProperty(valueName).SetValue(obj, newValue);
        }

    }

    public struct Value
    {
        public Type valueType;
        public string valueName;
        public object value;
        public object parent;

        public string ValueName => valueName;

        public Value(Type _valueType, string _valueName, object _value, object _parent)
        {
            valueType = _valueType;
            valueName = _valueName;
            value = _value;
            parent = _parent;
        }

    }
}
