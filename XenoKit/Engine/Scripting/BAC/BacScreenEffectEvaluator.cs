using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Xv2CoreLib.BPE;
using SimdVector4 = System.Numerics.Vector4;

namespace XenoKit.Engine.Scripting.BAC
{
    internal sealed class BacScreenEffectEvaluator
    {
        private readonly Actor character;

        public BacScreenEffectEvaluator(Actor character)
        {
            this.character = character;
        }

        public void Update(BacEntryInstance bacEntryInstance, int currentFrame)
        {
            BacScreenEffectState state = bacEntryInstance.ScreenEffectState;
            state.Clear();

            List<ushort> expiredEffects = null;
            int latestBodyOutlineStartFrame = int.MinValue;
            bool hasBodyOutline = false;

            foreach (BacScreenEffectInstance activeEffect in bacEntryInstance.ActiveScreenEffects)
            {
                BPE_Entry bpeEntry = activeEffect.Entry;
                int frame = currentFrame - activeEffect.StartFrame;
                bool isLooping = IsLooping(bpeEntry);

                if (!isLooping && bpeEntry.I_04 > 0 && frame > bpeEntry.I_04)
                {
                    if (expiredEffects == null)
                        expiredEffects = new List<ushort>();

                    expiredEffects.Add((ushort)bpeEntry.SortID);
                    continue;
                }

                int evaluationFrame = isLooping ? frame % bpeEntry.I_04 : frame;
                if (bpeEntry.SubEntries != null)
                {
                    foreach (BPE_SubEntry subEntry in bpeEntry.SubEntries)
                    {
                        if (!IsBpeSubEntryActive(subEntry, evaluationFrame))
                            continue;

                        switch ((int)subEntry.BpeType)
                        {
                            case 0:
                                UpdateBlurState(subEntry, evaluationFrame, state);
                                break;
                            case 1:
                                UpdateWhiteShineState(subEntry, evaluationFrame, state);
                                break;
                            case 2:
                                UpdateType2State(subEntry, evaluationFrame, state);
                                break;
                            case 3:
                                UpdateType3State(subEntry, evaluationFrame, state);
                                break;
                            case 6:
                                UpdateZoomState(subEntry, evaluationFrame, state);
                                break;
                            case 7:
                                UpdateType7State(subEntry, evaluationFrame, state);
                                break;
                            case 8:
                                UpdateHueState(subEntry, evaluationFrame, state);
                                break;
                        }
                    }
                }

                if (activeEffect.StartFrame >= latestBodyOutlineStartFrame &&
                    TryUpdateBodyOutline(bpeEntry, evaluationFrame))
                {
                    latestBodyOutlineStartFrame = activeEffect.StartFrame;
                    hasBodyOutline = true;
                }
            }

            if (expiredEffects != null)
            {
                foreach (ushort bpeIndex in expiredEffects)
                    bacEntryInstance.ClearScreenEffect(bpeIndex);
            }

            if (!hasBodyOutline)
                bacEntryInstance.ClearBodyOutlineValues();
        }

        public bool HasData(BPE_Entry bpeEntry)
        {
            if (bpeEntry?.SubEntries == null)
                return false;

            foreach (BPE_SubEntry subEntry in bpeEntry.SubEntries)
            {
                switch ((int)subEntry.BpeType)
                {
                    case 0:
                        if (subEntry.Type0?.Count > 0) return true;
                        break;
                    case 1:
                        if (subEntry.Type1?.Count > 0) return true;
                        break;
                    case 2:
                        if (subEntry.Type2?.Count > 0) return true;
                        break;
                    case 3:
                        if (subEntry.Type3?.Count > 0) return true;
                        break;
                    case 6:
                        if (subEntry.Type6?.Count > 0) return true;
                        break;
                    case 7:
                        if (subEntry.Type7?.Count > 0) return true;
                        break;
                    case 8:
                        if (subEntry.Type8?.Count > 0) return true;
                        break;
                    case 9:
                        if (subEntry.Type9?.Count > 0) return true;
                        break;
                }
            }

            return false;
        }

        private bool TryUpdateBodyOutline(BPE_Entry bpeEntry, int frame)
        {
            BPE_SubEntry bodyOutline = bpeEntry?.SubEntries?.FirstOrDefault(x => x.BpeType == BpeType.BodyOutline);

            if (bodyOutline?.Type9 == null || bodyOutline.Type9.Count == 0 ||
                frame < bodyOutline.I_04 || (bodyOutline.I_06 >= bodyOutline.I_04 && frame > bodyOutline.I_06))
            {
                return false;
            }

            BPE_Type9 previousKeyframe;
            BPE_Type9 nextKeyframe;
            float factor;
            if (!TryGetInterpolated(bodyOutline.Type9, frame, x => x.I_00, out previousKeyframe, out nextKeyframe, out factor))
                return false;

            float paletteIndex = 1f + character.ActorSlot;
            float transparency = MathHelper.Lerp(previousKeyframe.F_12, nextKeyframe.F_12, factor);
            const float baseRadius = 4f;
            const float transparencyRadiusScale = 3f;
            const float previewRadiusScale = 0.5f;
            float previewRadius = (baseRadius - transparencyRadiusScale * transparency) * previewRadiusScale;
            float previewTransparency = MathHelper.Clamp((baseRadius - previewRadius) / transparencyRadiusScale, 0f, 1f);
            character.ShaderParameters.BodyOutlineActive = true;
            character.ShaderParameters.BodyOutlineColor = new SimdVector4(
                MathHelper.Lerp(previousKeyframe.F_20, nextKeyframe.F_20, factor),
                MathHelper.Lerp(previousKeyframe.F_24, nextKeyframe.F_24, factor),
                MathHelper.Lerp(previousKeyframe.F_28, nextKeyframe.F_28, factor),
                MathHelper.Lerp(previousKeyframe.F_32, nextKeyframe.F_32, factor));
            character.ShaderParameters.BodyOutlineParam2 = new SimdVector4(paletteIndex / 256f, 0f, 1f, 0f);
            character.ShaderParameters.BodyOutlineParam3 = new SimdVector4(
                MathHelper.Lerp(previousKeyframe.F_04, nextKeyframe.F_04, factor),
                MathHelper.Lerp(previousKeyframe.F_08, nextKeyframe.F_08, factor),
                previewTransparency,
                MathHelper.Lerp(previousKeyframe.F_16, nextKeyframe.F_16, factor));

            return true;
        }

        private static bool IsBpeSubEntryActive(BPE_SubEntry subEntry, int frame)
        {
            if (frame < subEntry.I_04)
                return false;

            return subEntry.I_06 < subEntry.I_04 || frame <= subEntry.I_06;
        }

        private static bool IsLooping(BPE_Entry bpeEntry)
        {
            return bpeEntry != null && bpeEntry.I_00 == 1 && bpeEntry.I_04 > 0;
        }

        private static bool TryGetInterpolated<T>(IList<T> keyframes, int frame, Func<T, int> getFrame, out T previous, out T next, out float factor)
        {
            previous = default(T);
            next = default(T);
            factor = 0f;

            if (keyframes == null || keyframes.Count == 0)
                return false;

            previous = keyframes[0];
            next = previous;

            if (frame <= getFrame(previous))
                return true;

            for (int i = 1; i < keyframes.Count; i++)
            {
                next = keyframes[i];
                int previousFrame = getFrame(previous);
                int nextFrame = getFrame(next);

                if (frame <= nextFrame)
                {
                    if (nextFrame != previousFrame)
                        factor = (frame - previousFrame) / (float)(nextFrame - previousFrame);

                    return true;
                }

                previous = next;
            }

            return true;
        }

        private static void UpdateBlurState(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type0 previous;
            BPE_Type0 next;
            float factor;

            if (TryGetInterpolated(subEntry.Type0, frame, x => x.I_00, out previous, out next, out factor))
                state.BlurAmount = Math.Max(state.BlurAmount, Math.Abs(MathHelper.Lerp(previous.F_04, next.F_04, factor)));
        }

        private static void UpdateWhiteShineState(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type1 previous;
            BPE_Type1 next;
            float factor;

            if (TryGetInterpolated(subEntry.Type1, frame, x => x.I_00, out previous, out next, out factor))
            {
                float intensity = MathHelper.Lerp(previous.F_08, next.F_08, factor);
                float range = MathHelper.Lerp(previous.F_12, next.F_12, factor);
                float amount = range > 0f ? intensity / range : intensity;
                amount = MathHelper.Clamp(amount, 0f, 1f);
                state.WhiteShineAmount = Math.Max(state.WhiteShineAmount, amount);
            }
        }

        private static void UpdateType2State(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type2 previous;
            BPE_Type2 next;
            float factor;

            if (!TryGetInterpolated(subEntry.Type2, frame, x => x.I_00, out previous, out next, out factor))
                return;

            SimdVector4 multiplier = new SimdVector4(
                MathHelper.Lerp(previous.F_12, next.F_12, factor),
                MathHelper.Lerp(previous.F_16, next.F_16, factor),
                MathHelper.Lerp(previous.F_20, next.F_20, factor),
                MathHelper.Lerp(previous.F_24, next.F_24, factor));
            float amount = Math.Max(
                Math.Abs(MathHelper.Lerp(previous.F_40, next.F_40, factor)),
                Math.Max(
                    Math.Abs(MathHelper.Lerp(previous.F_44, next.F_44, factor)),
                    Math.Abs(MathHelper.Lerp(previous.F_48, next.F_48, factor))));

            if (amount < 0.0001f)
                amount = 1f;

            if (multiplier != SimdVector4.One)
            {
                SimdVector4 blendedMultiplier = SimdVector4.Lerp(SimdVector4.One, multiplier, MathHelper.Clamp(amount, 0f, 1f));
                state.ColorMultiply = SimdVector4.Multiply(state.ColorMultiply, blendedMultiplier);
                state.HasColorMultiply = true;
            }

            SimdVector4 color = new SimdVector4(
                MathHelper.Lerp(previous.F_28, next.F_28, factor),
                MathHelper.Lerp(previous.F_32, next.F_32, factor),
                MathHelper.Lerp(previous.F_36, next.F_36, factor),
                0f);

            if (color.X != 0f || color.Y != 0f || color.Z != 0f)
            {
                state.ColorAdd += color * MathHelper.Clamp(amount, 0f, 1f);
                state.HasColorAdd = true;
            }
        }

        private static void UpdateType3State(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type3 previous;
            BPE_Type3 next;
            float factor;

            if (!TryGetInterpolated(subEntry.Type3, frame, x => x.I_00, out previous, out next, out factor))
                return;

            SimdVector4 multiplier = new SimdVector4(
                MathHelper.Lerp(previous.F_08, next.F_08, factor),
                MathHelper.Lerp(previous.F_12, next.F_12, factor),
                MathHelper.Lerp(previous.F_16, next.F_16, factor),
                MathHelper.Lerp(previous.F_20, next.F_20, factor));

            state.ColorMultiply = SimdVector4.Multiply(state.ColorMultiply, multiplier);
            state.HasColorMultiply = true;
        }

        private static void UpdateZoomState(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type6 previous;
            BPE_Type6 next;
            float factor;

            if (!TryGetInterpolated(subEntry.Type6, frame, x => x.I_00, out previous, out next, out factor))
                return;

            float zoom = MathHelper.Lerp(previous.F_40, next.F_40, factor);
            if (Math.Abs(zoom) > Math.Abs(state.ZoomLevel))
                state.ZoomLevel = zoom;

            SimdVector4 color = new SimdVector4(
                MathHelper.Lerp(previous.F_08, next.F_08, factor),
                MathHelper.Lerp(previous.F_12, next.F_12, factor),
                MathHelper.Lerp(previous.F_16, next.F_16, factor),
                0f);
            float alpha = MathHelper.Clamp(MathHelper.Lerp(previous.F_20, next.F_20, factor), 0f, 1f);

            if (alpha > 0f && (color.X != 0f || color.Y != 0f || color.Z != 0f))
            {
                state.ColorAdd += color * alpha;
                state.HasColorAdd = true;
            }
        }

        private static void UpdateType7State(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type7 previous;
            BPE_Type7 next;
            float factor;

            if (TryGetInterpolated(subEntry.Type7, frame, x => x.I_00, out previous, out next, out factor))
            {
                float strength = MathHelper.Clamp(MathHelper.Lerp(previous.F_04, next.F_04, factor), 0f, 1f);
                state.HueStrength = Math.Min(state.HueStrength, strength);
            }
        }

        private static void UpdateHueState(BPE_SubEntry subEntry, int frame, BacScreenEffectState state)
        {
            BPE_Type8 previous;
            BPE_Type8 next;
            float factor;

            if (!TryGetInterpolated(subEntry.Type8, frame, x => x.I_00, out previous, out next, out factor))
                return;

            state.HueMode = factor < 0.5f ? previous.I_08 : next.I_08;
            state.HueColor = new SimdVector4(
                MathHelper.Lerp(previous.F_12, next.F_12, factor),
                MathHelper.Lerp(previous.F_16, next.F_16, factor),
                MathHelper.Lerp(previous.F_20, next.F_20, factor),
                MathHelper.Lerp(previous.F_24, next.F_24, factor));
            state.HasHue = true;
        }
    }
}
