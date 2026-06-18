using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace XenoKit.Editor
{
    public static class ClipboardConstants
    {
        //BAC
        public const string BacEntry_CopyItem = "XenoKit_BacEntryCopyItem";
        public const string BacType_CopyItem = "XenoKit_BacTypeCopyItem";

        //BSA/BCM
        public const string BsaEntry_CopyItem = "XenoKit_BsaEntryCopyItem";
        public const string BsaType_CopyItem = "XenoKit_BsaTypeCopyItem";
        public const string BcmEntry_CopyItem = "XenoKit_BcmEntryCopyItem";
        public const string BcmSubtrees_CopyItems = "XenoKit_BcmSubtreesCopyItems";

        //EAN
        public const string EanAnimation = "XenoKit_EanAnimation";
        public const string EanCameraAnimation = "XenoKit_EanCameraAnimation";
        public const string EanNode = "XenoKit_EanNode";
        public const string EanAnimationKeyframe = "XenoKit_SerialziedAnimationKeyframe";
        public const string CameraKeyframe = "XenoKit_SerialziedCameraKeyframe";

        //BCS
        public const string BcsPartSet = "XenoKit_BcsPartSet";
        public const string BcsPart = "XenoKit_BcsPart";
        public const string BcsPhysicsPart = "XenoKit_BcsPhysicsPart";
        public const string BcsColorSelector = "BcsColorSelector";
        public const string BcsColorGroup = "XenoKit_BcsColorGroup";
        public const string BcsColor = "XenoKit_BcsColor";
        public const string BcsBody = "XenoKit_BcsBody";
        public const string BcsBodyBone = "XenoKit_BcsBodyBone";
        public const string BcsCharaFile = "XenoKit_CharaFile";
        public const string BcsSkeletonDataBone = "XenoKit_BcsSkeletonDataBone";

        //EMD
        public const string EmdModel = "XenoKit_EmdModel";
        public const string EmdMesh = "XenoKit_EmdMesh";
        public const string EmdSubmesh = "XenoKit_EmdSubmesh";
        public const string EmdTextureSampler = "XenoKit_EmdTextureSampler";

        //EEPK and ACB handled elsewhere (in ACE/EEPK Org code)
    }

    public static class XenoKitClipboard
    {
        private static readonly Dictionary<string, ClipboardItem> items = new Dictionary<string, ClipboardItem>();

        public static void SetData(string format, object value)
        {
            string token = Guid.NewGuid().ToString("N");
            items[format] = new ClipboardItem(token, value);

            DataObject dataObject = new DataObject();
            dataObject.SetData(format, token, false);
            Clipboard.SetDataObject(dataObject, false);
        }

        public static bool ContainsData(string format)
        {
            return TryGetStoredItem(format, out _);
        }

        public static bool TryGetData<T>(string format, out T value) where T : class
        {
            value = null;

            if (!TryGetStoredItem(format, out ClipboardItem item))
                return false;

            if (!(item.Value is T typedValue))
                return false;

            value = Clone(typedValue) ?? typedValue;
            return true;
        }

        private static bool TryGetStoredItem(string format, out ClipboardItem item)
        {
            item = null;

            if (!items.TryGetValue(format, out ClipboardItem storedItem))
                return false;

            if (!HasClipboardToken(format, storedItem.Token))
            {
                items.Remove(format);
                return false;
            }

            item = storedItem;
            return true;
        }

        private static bool HasClipboardToken(string format, string token)
        {
            try
            {
                if (!Clipboard.ContainsData(format))
                    return false;

                return Clipboard.GetData(format) as string == token;
            }
            catch
            {
                return false;
            }
        }

        private static T Clone<T>(T value) where T : class
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, value);
                    stream.Position = 0;
                    return formatter.Deserialize(stream) as T;
                }
            }
            catch
            {
                return null;
            }
        }

        private class ClipboardItem
        {
            public ClipboardItem(string token, object value)
            {
                Token = token;
                Value = value;
            }

            public string Token { get; }
            public object Value { get; }
        }
    }
}
