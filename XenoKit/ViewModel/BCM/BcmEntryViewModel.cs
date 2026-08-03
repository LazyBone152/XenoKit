using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using Xv2CoreLib.BCM;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.ViewModel.BCM
{
    /// <summary>
    /// Wraps a single BCM_Entry. Replaces the old imperative BcmEntryEditor, which held edits in TextBox
    /// text and re-applied stale values over an undo. Every setter records an undo and every property is
    /// re-raised when undo or redo runs, so the editor can never disagree with the model.
    /// </summary>
    public class BcmEntryViewModel : ObservableObject, IDisposable
    {
        private const uint OpponentSizeFamilyMask = 0x000F0000;
        private const uint OpponentSizeUnknownMask = 0x0000000F;
        private const uint OpponentSizeUpgradeMask = 0xFF000000;

        // Narrowed from the old 0xFFFF so picking an option cannot wipe bits no choice covers.
        private const uint ActivatorModeMask = 0x0000001F;
        private const uint BacModeMask = 0x00000007;

        private readonly BCM_Entry entry;
        private readonly string structuralSiblingIndex;
        private readonly string structuralChildIndex;

        public event EventHandler EntryChanged;

        public BcmEntryViewModel(BCM_Entry entry, string siblingIndex, string childIndex)
        {
            this.entry = entry;
            structuralSiblingIndex = NormalizeLinkIndex(siblingIndex);
            structuralChildIndex = NormalizeLinkIndex(childIndex);

            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled += UndoManager_UndoOrRedoCalled;
        }

        public void Dispose()
        {
            if (UndoManager.Instance != null)
                UndoManager.Instance.UndoOrRedoCalled -= UndoManager_UndoOrRedoCalled;
        }

        private void UndoManager_UndoOrRedoCalled(object sender, EventArgs e)
        {
            UpdateProperties();
        }

        #region Directional Input
        public bool Dir_SingleActivation { get => HasDir(DirectionalInput.SingleActivation); set => SetDir(DirectionalInput.SingleActivation, value); }
        public bool Dir_Up { get => HasDir(DirectionalInput.Up); set => SetDir(DirectionalInput.Up, value); }
        public bool Dir_Down { get => HasDir(DirectionalInput.Down); set => SetDir(DirectionalInput.Down, value); }
        public bool Dir_Left { get => HasDir(DirectionalInput.Left); set => SetDir(DirectionalInput.Left, value); }
        public bool Dir_Right { get => HasDir(DirectionalInput.Right); set => SetDir(DirectionalInput.Right, value); }
        public bool Dir_Forward { get => HasDir(DirectionalInput.Forward); set => SetDir(DirectionalInput.Forward, value); }
        public bool Dir_Backwards { get => HasDir(DirectionalInput.Backwards); set => SetDir(DirectionalInput.Backwards, value); }
        public bool Dir_LeftRelative { get => HasDir(DirectionalInput.LeftRelative); set => SetDir(DirectionalInput.LeftRelative, value); }
        public bool Dir_RightRelative { get => HasDir(DirectionalInput.RightRelative); set => SetDir(DirectionalInput.RightRelative, value); }
        #endregion

        #region Button Input
        public bool Btn_LockOn { get => HasBtn(ButtonInput.LockOn); set => SetBtn(ButtonInput.LockOn, value); }
        public bool Btn_Descend { get => HasBtn(ButtonInput.Descend); set => SetBtn(ButtonInput.Descend, value); }
        public bool Btn_DragonRadar { get => HasBtn(ButtonInput.DragonRadar); set => SetBtn(ButtonInput.DragonRadar, value); }
        public bool Btn_Jump_2 { get => HasBtn(ButtonInput.Jump_2); set => SetBtn(ButtonInput.Jump_2, value); }
        public bool Btn_UltimateMenu { get => HasBtn(ButtonInput.UltimateMenu); set => SetBtn(ButtonInput.UltimateMenu, value); }
        public bool Btn_SkillInput { get => HasBtn(ButtonInput.SkillInput); set => SetBtn(ButtonInput.SkillInput, value); }
        public bool Btn_SuperMenuPlusSkillInput { get => HasBtn(ButtonInput.SuperMenuPlusSkillInput); set => SetBtn(ButtonInput.SuperMenuPlusSkillInput, value); }
        public bool Btn_UltimateMenuPlusSkillInput { get => HasBtn(ButtonInput.UltimateMenuPlusSkillInput); set => SetBtn(ButtonInput.UltimateMenuPlusSkillInput, value); }
        public bool Btn_Unk20 { get => HasBtn(ButtonInput.Unk20); set => SetBtn(ButtonInput.Unk20, value); }
        public bool Btn_UltimateSkill1 { get => HasBtn(ButtonInput.UltimateSkill1); set => SetBtn(ButtonInput.UltimateSkill1, value); }
        public bool Btn_UltimateSkill2 { get => HasBtn(ButtonInput.UltimateSkill2); set => SetBtn(ButtonInput.UltimateSkill2, value); }
        public bool Btn_AwokenSkill { get => HasBtn(ButtonInput.AwokenSkill); set => SetBtn(ButtonInput.AwokenSkill, value); }
        public bool Btn_EvasiveSkill { get => HasBtn(ButtonInput.EvasiveSkill); set => SetBtn(ButtonInput.EvasiveSkill, value); }
        public bool Btn_SuperSkill1 { get => HasBtn(ButtonInput.SuperSkill1); set => SetBtn(ButtonInput.SuperSkill1, value); }
        public bool Btn_SuperSkill2 { get => HasBtn(ButtonInput.SuperSkill2); set => SetBtn(ButtonInput.SuperSkill2, value); }
        public bool Btn_SuperSkill3 { get => HasBtn(ButtonInput.SuperSkill3); set => SetBtn(ButtonInput.SuperSkill3, value); }
        public bool Btn_SuperSkill4 { get => HasBtn(ButtonInput.SuperSkill4); set => SetBtn(ButtonInput.SuperSkill4, value); }
        public bool Btn_SkillMenu { get => HasBtn(ButtonInput.SkillMenu); set => SetBtn(ButtonInput.SkillMenu, value); }
        public bool Btn_Boost { get => HasBtn(ButtonInput.Boost); set => SetBtn(ButtonInput.Boost, value); }
        public bool Btn_Guard { get => HasBtn(ButtonInput.Guard); set => SetBtn(ButtonInput.Guard, value); }
        public bool Btn_Unk8 { get => HasBtn(ButtonInput.Unk8); set => SetBtn(ButtonInput.Unk8, value); }
        public bool Btn_Light { get => HasBtn(ButtonInput.Light); set => SetBtn(ButtonInput.Light, value); }
        public bool Btn_Heavy { get => HasBtn(ButtonInput.Heavy); set => SetBtn(ButtonInput.Heavy, value); }
        public bool Btn_Blast { get => HasBtn(ButtonInput.Blast); set => SetBtn(ButtonInput.Blast, value); }
        public bool Btn_Jump { get => HasBtn(ButtonInput.Jump); set => SetBtn(ButtonInput.Jump, value); }
        public bool Btn_Unk26 { get => HasBtn(ButtonInput.Unk26); set => SetBtn(ButtonInput.Unk26, value); }
        public bool Btn_Unk27 { get => HasBtn(ButtonInput.Unk27); set => SetBtn(ButtonInput.Unk27, value); }
        public bool Btn_Unk28 { get => HasBtn(ButtonInput.Unk28); set => SetBtn(ButtonInput.Unk28, value); }
        public bool Btn_UltimateMenu_2 { get => HasBtn(ButtonInput.UltimateMenu_2); set => SetBtn(ButtonInput.UltimateMenu_2, value); }
        public bool Btn_Unk30 { get => HasBtn(ButtonInput.Unk30); set => SetBtn(ButtonInput.Unk30, value); }
        public bool Btn_Unk31 { get => HasBtn(ButtonInput.Unk31); set => SetBtn(ButtonInput.Unk31, value); }
        public bool Btn_Unk32 { get => HasBtn(ButtonInput.Unk32); set => SetBtn(ButtonInput.Unk32, value); }
        #endregion

        #region Hold Down Conditions
        public string HoldDownConditionsHex => $"0x{entry.HoldDownConditions:X}";

        public uint HoldDown_ChargeType { get => GetMasked(entry.HoldDownConditions, 0x00030000, 16); set => SetHoldDown(0x00030000, 16, value); }
        public uint HoldDown_Options2 { get => GetMasked(entry.HoldDownConditions, 0x0000F000, 12); set => SetHoldDown(0x0000F000, 12, value); }
        public uint HoldDown_Options3 { get => GetMasked(entry.HoldDownConditions, 0x00000F00, 8); set => SetHoldDown(0x00000F00, 8, value); }
        public uint HoldDown_Options4 { get => GetMasked(entry.HoldDownConditions, 0x000000F0, 4); set => SetHoldDown(0x000000F0, 4, value); }
        public uint HoldDown_Behaviour { get => GetMasked(entry.HoldDownConditions, 0x0000000F, 0); set => SetHoldDown(0x0000000F, 0, value); }
        #endregion

        #region Activator Mode and Opponent Size
        public string ActivatorModeHex => $"0x{entry.I_00:X}";

        public uint ActivatorMode
        {
            get => entry.I_00 & ActivatorModeMask;
            set
            {
                uint newValue = (entry.I_00 & ~ActivatorModeMask) | (value & ActivatorModeMask);
                if (newValue == entry.I_00) return;

                SetValue(nameof(entry.I_00), entry.I_00, newValue, v => entry.I_00 = v, "BCM Activator Mode");
                RaisePropertyChanged(() => ActivatorModeHex);
                RaisePropertyChanged(() => IsUpgradeLevelMode);
                RaisePropertyChanged(() => UpgradeLevelVisibility);
                RaisePropertyChanged(() => OpponentSizeVisibility);
            }
        }

        /// <summary>Mode 1 repurposes OpponentSizeConditions as a skill upgrade level.</summary>
        public bool IsUpgradeLevelMode => entry.I_00 == 1;
        public Visibility UpgradeLevelVisibility => IsUpgradeLevelMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OpponentSizeVisibility => IsUpgradeLevelMode ? Visibility.Collapsed : Visibility.Visible;

        public uint OpponentSizeFamily { get => entry.OpponentSizeConditions & OpponentSizeFamilyMask; set => SetOpponentSizeBits(OpponentSizeFamilyMask, value, "BCM Opponent Size"); }
        public uint OpponentSizeUnknown { get => entry.OpponentSizeConditions & OpponentSizeUnknownMask; set => SetOpponentSizeBits(OpponentSizeUnknownMask, value, "BCM Opponent Size Unknown"); }

        public uint UpgradeLevel
        {
            get => (entry.OpponentSizeConditions & OpponentSizeUpgradeMask) >> 24;
            set => SetOpponentSizeBits(OpponentSizeUpgradeMask, (value << 24) & OpponentSizeUpgradeMask, "BCM Upgrade Level");
        }

        public string OpponentSizeConditionsHex => $"0x{entry.OpponentSizeConditions:X}";
        #endregion

        #region Loop, Resource, User State
        public ushort MinimumLoopDuration { get => entry.MinimumLoopDuration; set => SetValue(nameof(entry.MinimumLoopDuration), entry.MinimumLoopDuration, value, v => entry.MinimumLoopDuration = v, "BCM Minimum Loop Duration"); }
        public ushort MaximumLoopDuration { get => entry.MaximumLoopDuration; set => SetValue(nameof(entry.MaximumLoopDuration), entry.MaximumLoopDuration, value, v => entry.MaximumLoopDuration = v, "BCM Maximum Loop Duration"); }

        public uint ReceiverLinkID { get => entry.ReceiverLinkID; set => SetValue(nameof(entry.ReceiverLinkID), entry.ReceiverLinkID, value, v => entry.ReceiverLinkID = v, "BCM Receiver Link Id"); }

        public uint KiCost { get => entry.I_64; set => SetValue(nameof(entry.I_64), entry.I_64, value, v => entry.I_64 = v, "BCM Ki Cost"); }
        public uint StaminaCost { get => entry.StaminaCost; set => SetValue(nameof(entry.StaminaCost), entry.StaminaCost, value, v => entry.StaminaCost = v, "BCM Stamina Cost"); }
        public uint KiRequired { get => entry.KiRequired; set => SetValue(nameof(entry.KiRequired), entry.KiRequired, value, v => entry.KiRequired = v, "BCM Ki Required"); }
        public float HealthRequired { get => entry.HealthRequired; set => SetValue(nameof(entry.HealthRequired), entry.HealthRequired, value, v => entry.HealthRequired = v, "BCM Health Required"); }

        public short CusAura { get => entry.CusAura; set => SetValue(nameof(entry.CusAura), entry.CusAura, value, v => entry.CusAura = v, "BCM CUS Aura"); }
        public short TransStage { get => entry.TransStage; set => SetValue(nameof(entry.TransStage), entry.TransStage, value, v => entry.TransStage = v, "BCM Transformation Stage"); }
        #endregion

        #region Primary Activator Conditions
        public bool Pri_TargetsHealthLessThan25 { get => HasPri(PrimaryActivatorConditions.TargetsHealthLessThan25); set => SetPri(PrimaryActivatorConditions.TargetsHealthLessThan25, value); }
        public bool Pri_OpponentKnockback { get => HasPri(PrimaryActivatorConditions.OpponentKnockback); set => SetPri(PrimaryActivatorConditions.OpponentKnockback, value); }
        public bool Pri_TargetingOpponent { get => HasPri(PrimaryActivatorConditions.TargetingOpponent); set => SetPri(PrimaryActivatorConditions.TargetingOpponent, value); }
        public bool Pri_OnAttackHit { get => HasPri(PrimaryActivatorConditions.OnAttackHit); set => SetPri(PrimaryActivatorConditions.OnAttackHit, value); }
        public bool Pri_CurrentBacEntryHits { get => HasPri(PrimaryActivatorConditions.CurrentBacEntryHits); set => SetPri(PrimaryActivatorConditions.CurrentBacEntryHits, value); }
        public bool Pri_AttackBlocked { get => HasPri(PrimaryActivatorConditions.AttackBlocked); set => SetPri(PrimaryActivatorConditions.AttackBlocked, value); }
        public bool Pri_ActiveProjectile { get => HasPri(PrimaryActivatorConditions.ActiveProjectile); set => SetPri(PrimaryActivatorConditions.ActiveProjectile, value); }
        public bool Pri_UsersHealth_OneUse { get => HasPri(PrimaryActivatorConditions.UsersHealth_OneUse); set => SetPri(PrimaryActivatorConditions.UsersHealth_OneUse, value); }
        public bool Pri_UsersHealth { get => HasPri(PrimaryActivatorConditions.UsersHealth); set => SetPri(PrimaryActivatorConditions.UsersHealth, value); }
        public bool Pri_InTransformedState { get => HasPri(PrimaryActivatorConditions.InTransformedState); set => SetPri(PrimaryActivatorConditions.InTransformedState, value); }
        public bool Pri_InBaseForm { get => HasPri(PrimaryActivatorConditions.InBaseForm); set => SetPri(PrimaryActivatorConditions.InBaseForm, value); }
        public bool Pri_Idle { get => HasPri(PrimaryActivatorConditions.Idle); set => SetPri(PrimaryActivatorConditions.Idle, value); }
        public bool Pri_Unk10 { get => HasPri(PrimaryActivatorConditions.Unk10); set => SetPri(PrimaryActivatorConditions.Unk10, value); }
        public bool Pri_Standing { get => HasPri(PrimaryActivatorConditions.Standing); set => SetPri(PrimaryActivatorConditions.Standing, value); }
        public bool Pri_Floating { get => HasPri(PrimaryActivatorConditions.Floating); set => SetPri(PrimaryActivatorConditions.Floating, value); }
        public bool Pri_TouchingGround { get => HasPri(PrimaryActivatorConditions.TouchingGround); set => SetPri(PrimaryActivatorConditions.TouchingGround, value); }
        public bool Pri_CloseToTarget { get => HasPri(PrimaryActivatorConditions.CloseToTarget); set => SetPri(PrimaryActivatorConditions.CloseToTarget, value); }
        public bool Pri_FarFromTarget { get => HasPri(PrimaryActivatorConditions.FarFromTarget); set => SetPri(PrimaryActivatorConditions.FarFromTarget, value); }
        public bool Pri_NotNearStageCeiling { get => HasPri(PrimaryActivatorConditions.NotNearStageCeiling); set => SetPri(PrimaryActivatorConditions.NotNearStageCeiling, value); }
        public bool Pri_NotNearCertainObjects { get => HasPri(PrimaryActivatorConditions.NotNearCertainObjects); set => SetPri(PrimaryActivatorConditions.NotNearCertainObjects, value); }
        public bool Pri_StaminaAboveZero { get => HasPri(PrimaryActivatorConditions.StaminaAboveZero); set => SetPri(PrimaryActivatorConditions.StaminaAboveZero, value); }
        public bool Pri_CounterProjectile { get => HasPri(PrimaryActivatorConditions.CounterProjectile); set => SetPri(PrimaryActivatorConditions.CounterProjectile, value); }
        public bool Pri_KiBelow100 { get => HasPri(PrimaryActivatorConditions.KiBelow100); set => SetPri(PrimaryActivatorConditions.KiBelow100, value); }
        public bool Pri_KiAboveZero { get => HasPri(PrimaryActivatorConditions.KiAboveZero); set => SetPri(PrimaryActivatorConditions.KiAboveZero, value); }
        public bool Pri_Unk17 { get => HasPri(PrimaryActivatorConditions.Unk17); set => SetPri(PrimaryActivatorConditions.Unk17, value); }
        public bool Pri_Unk18 { get => HasPri(PrimaryActivatorConditions.Unk18); set => SetPri(PrimaryActivatorConditions.Unk18, value); }
        public bool Pri_CounterMelee { get => HasPri(PrimaryActivatorConditions.CounterMelee); set => SetPri(PrimaryActivatorConditions.CounterMelee, value); }
        public bool Pri_Ground { get => HasPri(PrimaryActivatorConditions.Ground); set => SetPri(PrimaryActivatorConditions.Ground, value); }
        public bool Pri_Opponent { get => HasPri(PrimaryActivatorConditions.Opponent); set => SetPri(PrimaryActivatorConditions.Opponent, value); }
        public bool Pri_Unk11 { get => HasPri(PrimaryActivatorConditions.Unk11); set => SetPri(PrimaryActivatorConditions.Unk11, value); }
        public bool Pri_Unk22 { get => HasPri(PrimaryActivatorConditions.Unk22); set => SetPri(PrimaryActivatorConditions.Unk22, value); }
        public bool Pri_Unk24 { get => HasPri(PrimaryActivatorConditions.Unk24); set => SetPri(PrimaryActivatorConditions.Unk24, value); }
        #endregion

        #region Activator State
        public bool State_ReceivingDamage { get => HasState(ActivatorState.ReceivingDamage); set => SetState(ActivatorState.ReceivingDamage, value); }
        public bool State_Jumping { get => HasState(ActivatorState.Jumping); set => SetState(ActivatorState.Jumping, value); }
        public bool State_NotReceivingDamage { get => HasState(ActivatorState.NotReceivingDamage); set => SetState(ActivatorState.NotReceivingDamage, value); }
        public bool State_TargetIsAttacking { get => HasState(ActivatorState.TargetIsAttacking); set => SetState(ActivatorState.TargetIsAttacking, value); }
        public bool State_Idle { get => HasState(ActivatorState.Idle); set => SetState(ActivatorState.Idle, value); }
        public bool State_Attacking { get => HasState(ActivatorState.Attacking); set => SetState(ActivatorState.Attacking, value); }
        public bool State_Boosting { get => HasState(ActivatorState.Boosting); set => SetState(ActivatorState.Boosting, value); }
        public bool State_Guarding { get => HasState(ActivatorState.Guarding); set => SetState(ActivatorState.Guarding, value); }
        #endregion

        #region BAC
        public short BacEntryPrimary { get => entry.BacEntryPrimary; set => SetValue(nameof(entry.BacEntryPrimary), entry.BacEntryPrimary, value, v => entry.BacEntryPrimary = v, "BCM Primary BAC"); }
        public short BacEntryCharge { get => entry.BacEntryCharge; set => SetValue(nameof(entry.BacEntryCharge), entry.BacEntryCharge, value, v => entry.BacEntryCharge = v, "BCM Charge BAC"); }
        public short BacEntryUserConnect { get => entry.BacEntryUserConnect; set => SetValue(nameof(entry.BacEntryUserConnect), entry.BacEntryUserConnect, value, v => entry.BacEntryUserConnect = v, "BCM User Connect BAC"); }
        public short BacEntryVictimConnect { get => entry.BacEntryVictimConnect; set => SetValue(nameof(entry.BacEntryVictimConnect), entry.BacEntryVictimConnect, value, v => entry.BacEntryVictimConnect = v, "BCM Victim Connect BAC"); }
        public short BacEntryAirborne { get => entry.BacEntryAirborne; set => SetValue(nameof(entry.BacEntryAirborne), entry.BacEntryAirborne, value, v => entry.BacEntryAirborne = v, "BCM Airborne BAC"); }
        public ushort BacEntryTargetingOverride { get => entry.BacEntryTargetingOverride; set => SetValue(nameof(entry.BacEntryTargetingOverride), entry.BacEntryTargetingOverride, value, v => entry.BacEntryTargetingOverride = v, "BCM Targeting Override BAC"); }

        public string BacModeHex => $"0x{entry.RandomFlag:X}";

        public uint BacMode
        {
            get => (uint)(entry.RandomFlag & BacModeMask);
            set
            {
                ushort newValue = (ushort)((entry.RandomFlag & ~BacModeMask) | (value & BacModeMask));
                if (newValue == entry.RandomFlag) return;

                SetValue(nameof(entry.RandomFlag), entry.RandomFlag, newValue, v => entry.RandomFlag = v, "BCM BAC Mode");
                RaisePropertyChanged(() => BacModeHex);
            }
        }
        #endregion

        #region Misc links
        /// <summary>
        /// Shows the real tree link. A loop override is only written when the user changes it away from
        /// the structural index, which is why the model value can be null.
        /// </summary>
        public string SiblingIndex
        {
            get => string.IsNullOrWhiteSpace(entry.LoopAsSibling) || NormalizeLinkIndex(entry.LoopAsSibling) == "0" ? structuralSiblingIndex : NormalizeLinkIndex(entry.LoopAsSibling);
            set => SetLink(nameof(entry.LoopAsSibling), entry.LoopAsSibling, value, structuralSiblingIndex, v => entry.LoopAsSibling = v, "BCM Sibling Index");
        }

        public string ChildIndex
        {
            get => string.IsNullOrWhiteSpace(entry.LoopAsChild) || NormalizeLinkIndex(entry.LoopAsChild) == "0" ? structuralChildIndex : NormalizeLinkIndex(entry.LoopAsChild);
            set => SetLink(nameof(entry.LoopAsChild), entry.LoopAsChild, value, structuralChildIndex, v => entry.LoopAsChild = v, "BCM Child Index");
        }
        #endregion

        #region Unknown
        public string I_00Hex { get => ToHex(entry.I_00); set => SetHex(nameof(entry.I_00), entry.I_00, value, v => entry.I_00 = v, "BCM I_00"); }
        public string I_36Hex { get => ToHex(unchecked((ushort)entry.I_36)); set => SetHexShort(nameof(entry.I_36), entry.I_36, value, v => entry.I_36 = v, "BCM I_36"); }
        public string I_68Hex { get => ToHex(entry.I_68); set => SetHex(nameof(entry.I_68), entry.I_68, value, v => entry.I_68 = v, "BCM I_68"); }
        public string I_72Hex { get => ToHex(entry.I_72); set => SetHex(nameof(entry.I_72), entry.I_72, value, v => entry.I_72 = v, "BCM I_72"); }
        public string I_80Hex { get => ToHex(entry.I_80); set => SetHex(nameof(entry.I_80), entry.I_80, value, v => entry.I_80 = v, "BCM I_80"); }
        public string I_88Hex { get => ToHex(entry.I_88); set => SetHex(nameof(entry.I_88), entry.I_88, value, v => entry.I_88 = v, "BCM I_88"); }
        public string I_104Hex
        {
            get => ToHex(entry.I_104);
            set
            {
                SetHex(nameof(entry.I_104), entry.I_104, value, v => entry.I_104 = v, "BCM I_104");
                RaiseI104Properties();
            }
        }

        public bool I104_MinimumSkillUpgradeLevel { get => HasI104(0x00000001); set => SetI104(0x00000001, value); }
        public uint CharacterCondition
        {
            get => entry.I_108;
            set
            {
                SetValue(nameof(entry.I_108), entry.I_108, value, v => entry.I_108 = v, "BCM Character Condition");
            }
        }
        #endregion

        #region Plumbing
        private bool HasDir(DirectionalInput flag) => (entry.DirectionalInput & flag) == flag;
        private bool HasBtn(ButtonInput flag) => (entry.ButtonInput & flag) == flag;
        private bool HasPri(PrimaryActivatorConditions flag) => (entry.PrimaryActivatorConditions & flag) == flag;
        private bool HasState(ActivatorState flag) => (entry.ActivatorState & flag) == flag;
        private bool HasI104(uint mask) => (entry.I_104 & mask) != 0;

        private void SetI104(uint mask, bool state, [CallerMemberName] string viewModelProperty = null)
        {
            uint newValue = state ? entry.I_104 | mask : entry.I_104 & ~mask;
            if (newValue == entry.I_104) return;

            SetValue(nameof(entry.I_104), entry.I_104, newValue, v => entry.I_104 = v, "BCM I_104 Conditions", viewModelProperty);
            RaiseI104Properties();
        }

        private void RaiseI104Properties()
        {
            RaisePropertyChanged(() => I_104Hex);
            RaisePropertyChanged(() => I104_MinimumSkillUpgradeLevel);
        }

        private void SetDir(DirectionalInput flag, bool state, [CallerMemberName] string viewModelProperty = null)
        {
            DirectionalInput newValue = state ? entry.DirectionalInput | flag : entry.DirectionalInput & ~flag;
            SetValue(nameof(entry.DirectionalInput), entry.DirectionalInput, newValue, v => entry.DirectionalInput = v, "BCM Directional Input", viewModelProperty);
        }

        private void SetBtn(ButtonInput flag, bool state, [CallerMemberName] string viewModelProperty = null)
        {
            ButtonInput newValue = state ? entry.ButtonInput | flag : entry.ButtonInput & ~flag;
            SetValue(nameof(entry.ButtonInput), entry.ButtonInput, newValue, v => entry.ButtonInput = v, "BCM Button Input", viewModelProperty);
        }

        private void SetPri(PrimaryActivatorConditions flag, bool state, [CallerMemberName] string viewModelProperty = null)
        {
            PrimaryActivatorConditions newValue = state ? entry.PrimaryActivatorConditions | flag : entry.PrimaryActivatorConditions & ~flag;
            SetValue(nameof(entry.PrimaryActivatorConditions), entry.PrimaryActivatorConditions, newValue, v => entry.PrimaryActivatorConditions = v, "BCM Primary Conditions", viewModelProperty);
        }

        private void SetState(ActivatorState flag, bool state, [CallerMemberName] string viewModelProperty = null)
        {
            ActivatorState newValue = state ? entry.ActivatorState | flag : entry.ActivatorState & ~flag;
            SetValue(nameof(entry.ActivatorState), entry.ActivatorState, newValue, v => entry.ActivatorState = v, "BCM Activator State", viewModelProperty);
        }

        private static uint GetMasked(uint source, uint mask, int shift) => (source & mask) >> shift;

        private void SetHoldDown(uint mask, int shift, uint value, [CallerMemberName] string viewModelProperty = null)
        {
            uint newValue = (entry.HoldDownConditions & ~mask) | ((value << shift) & mask);
            if (newValue == entry.HoldDownConditions) return;

            SetValue(nameof(entry.HoldDownConditions), entry.HoldDownConditions, newValue, v => entry.HoldDownConditions = v, "BCM Hold Down Conditions", viewModelProperty);
            RaisePropertyChanged(() => HoldDownConditionsHex);
        }

        private void SetOpponentSizeBits(uint mask, uint value, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            uint newValue = (entry.OpponentSizeConditions & ~mask) | (value & mask);
            if (newValue == entry.OpponentSizeConditions) return;

            SetValue(nameof(entry.OpponentSizeConditions), entry.OpponentSizeConditions, newValue, v => entry.OpponentSizeConditions = v, undoName, viewModelProperty);
            RaisePropertyChanged(() => OpponentSizeConditionsHex);
        }

        private void SetLink(string modelProperty, string oldValue, string newText, string structuralIndex, Action<string> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            string normalized = NormalizeLinkIndex(newText);
            string loopValue = normalized == structuralIndex ? null : normalized;

            SetValue(modelProperty, oldValue, loopValue, assign, undoName, viewModelProperty);
        }

        private static string ToHex(uint value) => $"0x{value:X}";

        private void SetHex(string modelProperty, uint oldValue, string text, Action<uint> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (!TryParseHex(text, out uint parsed))
            {
                RaisePropertyChanged(viewModelProperty);
                return;
            }

            SetValue(modelProperty, oldValue, parsed, assign, undoName, viewModelProperty);
        }

        private void SetHexShort(string modelProperty, short oldValue, string text, Action<short> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (!TryParseHex(text, out uint parsed))
            {
                RaisePropertyChanged(viewModelProperty);
                return;
            }

            SetValue(modelProperty, oldValue, unchecked((short)parsed), assign, undoName, viewModelProperty);
        }

        private static bool TryParseHex(string text, out uint result)
        {
            text = (text ?? string.Empty).Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return uint.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);

            return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static string NormalizeLinkIndex(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "0" : value.Trim();
        }

        private void SetValue<TValue>(string modelProperty, TValue oldValue, TValue newValue, Action<TValue> assign, string undoName, [CallerMemberName] string viewModelProperty = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(oldValue, newValue)) return;

            UndoManager.Instance.AddUndo(new UndoableProperty<BCM_Entry>(modelProperty, entry, oldValue, newValue, undoName));
            assign(newValue);
            RaisePropertyChanged(viewModelProperty);
            EntryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Undo and redo write straight to the model, so everything has to be re-read.
        /// </summary>
        private void UpdateProperties()
        {
            RaisePropertyChanged(string.Empty);
        }
        #endregion
    }
}
