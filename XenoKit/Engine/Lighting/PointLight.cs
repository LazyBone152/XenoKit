using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xv2CoreLib.EEPK;

namespace XenoKit.Engine.Lighting
{
    public class PointLight
    {
        public Vector3 Position { get; private set; }
        public Vector4 Color { get; private set; }
        public float RangeStart { get; private set; }
        public float RangeEnd { get; private set; }
        public EffectPart Effect { get; private set; }
        public Actor Owner { get; private set; }
        public ulong UpdateTick { get; private set; }

        public void Reset(bool resetAll = true)
        {
            Position = Vector3.Zero;
            Color = Vector4.Zero;
            RangeStart = 0;
            RangeEnd = 0;

            if (resetAll)
            {
                Effect = null;
                Owner = null;
                UpdateTick = 0;
            }
        }

        public void SetInitialState(Actor owner, EffectPart effect)
        {
            Effect = effect;
            Owner = owner;
        }

        public void SetValues(in Vector3 position, in Vector4 color, float rangeStart, float rangeEnd)
        {
            Position = position;
            Color = color;
            RangeEnd = rangeEnd;
            RangeStart = rangeStart;
            UpdateTick = Viewport.Instance.Tick;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FlagCheck(EepkEffectPartFlags2 flag)
        {
            byte maskedFlag = (byte)(Effect.Flags2 & (EepkEffectPartFlags2.LightApplyToCharacter | EepkEffectPartFlags2.LightApplyToStage | EepkEffectPartFlags2.LightApplyToVfx));

            return maskedFlag == 0 || (maskedFlag & (byte)flag) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OwnerCheck(Actor actor)
        {
            return actor is null || actor == Owner;
        }

        public bool CalcLuminanceCharacter(Vector3 target, out float luminance, out float attenuation)
        {
            attenuation = 1f;
            luminance = 0f;
            float distance = Vector3.Distance(target, Position) - 1f; //Subtract a hardcoded radius of 1

            if (RangeStart < distance)
                attenuation = distance < RangeEnd ? 1f - (distance - RangeStart) / (RangeEnd - RangeStart) : 0f;

            if (attenuation <= 0f)
                return false;

            luminance = MathF.Max(EngineUtils.CalculateLuminance(Color), 0) * attenuation;
            return true;
        }

        public bool CalcLuminanceEffect(Vector3 target, float radius, out float luminance, out float attenuation)
        {
            float distance = Vector3.Distance(target, Position);

            if (!DistanceAttenuation(distance, radius, out attenuation))
            {
                luminance = 0f;
                attenuation = 0f;
                return false;
            }

            //No specular color for point lights
            luminance = MathF.Max(EngineUtils.CalculateLuminance(Color), 0) * attenuation;
            return true;
        }

        private bool DistanceAttenuation(float dist, float boundingRadius, out float attenuation)
        {
            float distance = dist - boundingRadius;

            if (distance <= RangeStart) 
            {
                attenuation = 1f;
                return true; 
            }

            if (RangeEnd <= distance) 
            { 
                attenuation = 0f; 
                return false; 
            }

            const float FLT_EPSILON = 1.1920929e-7f;
            float range = RangeEnd - RangeStart;
            float t = MathF.Abs(range) <= FLT_EPSILON ? (distance - RangeStart >= FLT_EPSILON ? 0f : 1f) : (distance - RangeStart) / range;
            t *= t;
            attenuation = 1f - t;

            return attenuation > 0f;
        }
    }
}
