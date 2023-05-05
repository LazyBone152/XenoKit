using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xv2CoreLib;
using Xv2CoreLib.ACB;

namespace XenoKit.Helper
{
    public static class GeneralHelpers
    {
        public static int AssignCommonCueId(IList<Xv2File<ACB_Wrapper>> acbFiles, int min = 50)
        {
            while (CueIdUsed(acbFiles, min))
                min++;

            return min;
        }

        private static bool CueIdUsed(IList<Xv2File<ACB_Wrapper>> acbFiles, int cueId)
        {
            foreach (var file in acbFiles)
                if (file.File.AcbFile.Cues.Exists(x => x.ID == cueId)) return true;

            return false;
        }

        public static bool IsIdUsed<T>(IList<T> entries, T currentEntry, int id) where T : class, IInstallable, new()
        {
            foreach(var entry in entries)
            {
                if (entry.SortID == id && entry != currentEntry) return true;
            }

            return false;
        }

        public static Matrix SlerpMatrix(Matrix start, Matrix end, float slerpAmount)
        {
            Matrix result;
            Quaternion qStart, qEnd, qResult;
            Vector3 curTrans, nextTrans, lerpedTrans, curScale, nextScale, lerpedScale;

            start.Decompose(out curScale, out qStart, out curTrans);
            end.Decompose(out nextScale, out qEnd, out nextTrans);

            Quaternion.Lerp(ref qStart, ref qEnd, slerpAmount, out qResult);
            Vector3.Lerp(ref curTrans, ref nextTrans, slerpAmount, out lerpedTrans);
            Vector3.Lerp(ref curScale, ref nextScale, slerpAmount, out lerpedScale);

            result = Matrix.CreateScale(lerpedScale)
                   * Matrix.CreateFromQuaternion(qResult)
                   * Matrix.CreateTranslation(lerpedTrans);
            return result;
        }

        public static Quaternion EulerAnglesToQuaternion(Vector3 eulerAngles)
        {
            //https://stackoverflow.com/questions/70462758/c-sharp-how-to-convert-quaternions-to-euler-angles-xyz

            float cy = (float)Math.Cos(eulerAngles.Z * 0.5);
            float sy = (float)Math.Sin(eulerAngles.Z * 0.5);
            float cp = (float)Math.Cos(eulerAngles.Y * 0.5);
            float sp = (float)Math.Sin(eulerAngles.Y * 0.5);
            float cr = (float)Math.Cos(eulerAngles.X * 0.5);
            float sr = (float)Math.Sin(eulerAngles.X * 0.5);

            return new Quaternion
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };

        }
    }
}
