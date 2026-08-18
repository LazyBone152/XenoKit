using System;
using System.Reflection;

namespace XenoKit.Editor
{
    internal struct ReplacedId
    {
        public object IdObject { get; set; }
        public int IdIndex { get; set; } //For use when there are multiple references on an object (such as BDM Entries whcib have an acb, 3 effects, another bdm entry...)

        //BDM:
        //0 = ACB, 1 = Effect1, 2 = Effect2, 3 = Effect3, 4 = StaminaBrokenOverrideBdmId

        public ReplacedId(object idObect)
        {
            IdObject = idObect;
            IdIndex = 0;
        }

        public ReplacedId(object idObect, int idIndex)
        {
            IdObject = idObect;
            IdIndex = idIndex;
        }
    }

    internal class ValueReference
    {
        internal enum Mode
        {
            Id,
            Type,
            SkillId
        }

        internal enum InstanceRefType
        {
            Bac,
            Bcm,
            Bdm,
            ShotBdm,
            Bsa,
            SeAcb,
            Ean,
            Cam,
            Eepk
        }

        public object Instance;
        public string propName;
        public InstanceRefType RefType;
        public Mode mode;

        //Mode specific:
        public int oldId;

        public ValueReference(object instance, string _propName, InstanceRefType refType, Mode _mode = Mode.Id)
        {
            Instance = instance;
            propName = _propName;
            object numObj = GetProperty().GetValue(Instance);
            oldId = Convert.ToInt32(numObj);
            RefType = refType;
            mode = _mode;

        }

        public void SetEnum(int newValue)
        {
            PropertyInfo property = GetProperty();
            Type propertyType = property.PropertyType;

            if (!propertyType.IsEnum)
                throw new InvalidOperationException($"ValueReference.SetEnum: Invalid PropertyType = {propertyType}");

            property.SetValue(Instance, Enum.ToObject(propertyType, newValue), null);
        }

        public void ReplaceValue(int newValue)
        {
            PropertyInfo property = GetProperty();
            object value = CreateReplacementValue(property.PropertyType, newValue);
            property.SetValue(Instance, value, null);
        }

        private PropertyInfo GetProperty()
        {
            PropertyInfo property = Instance.GetType().GetProperty(propName);
            if (property == null)
                throw new InvalidOperationException($"ValueReference: Property not found = {propName}");

            return property;
        }

        private static object CreateReplacementValue(Type type, int newValue)
        {
            if (type.IsEnum)
                return Enum.ToObject(type, newValue);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int32:
                    return newValue;
                case TypeCode.UInt32:
                    return unchecked((uint)newValue);
                case TypeCode.UInt16:
                    return unchecked((ushort)newValue);
                case TypeCode.Int16:
                    return unchecked((short)newValue);
                default:
                    throw new InvalidOperationException($"ValueReference.ReplaceValue: Invalid PropertyType = {type}");
            }
        }
    }

}
