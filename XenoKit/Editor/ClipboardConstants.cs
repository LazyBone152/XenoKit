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
        public const string BsaCollision_CopyItem = "XenoKit_BsaCollisionCopyItem";
        public const string BsaExpiration_CopyItem = "XenoKit_BsaExpirationCopyItem";
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
        public static void SetData(string format, object value)
        {
            Clipboard.SetData(format, value);
        }

        public static bool ContainsData(string format)
        {
            return Clipboard.ContainsData(format);
        }

        public static bool TryGetData<T>(string format, out T value) where T : class
        {
            if(!Clipboard.ContainsData(format))
            {
                value = null;
                return false;
            }
            else
            {
                try
                {
                    value = Clipboard.GetData(format) as T;
                    return true;
                }
                catch
                {
                    value = null;
                    Log.Add("Failed to retrieve data from clipboard for format: " + format, LogType.Error);
                    return false;
                }
            }
        }
    }
}