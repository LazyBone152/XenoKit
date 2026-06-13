using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xv2CoreLib.BCM;
using Xv2CoreLib.Resource.UndoRedo;

namespace XenoKit.Views.BCM
{
    public partial class BcmEntryEditor : UserControl
    {
        private void BuildInputsTab()
        {
            AddBitGroup(inputsPanel, "Directional Input", nameof(BCM_Entry.DirectionalInput),
                new BcmChoiceGroup("User Direction",
                    Choice("Input activated once", DirectionalInput.SingleActivation),
                    Choice("Up", DirectionalInput.Up),
                    Choice("Down", DirectionalInput.Down),
                    Choice("Left", DirectionalInput.Left),
                    Choice("Right", DirectionalInput.Right)),
                new BcmChoiceGroup("Actor Direction",
                    Choice("Forwards", DirectionalInput.Forward),
                    Choice("Backwards", DirectionalInput.Backwards),
                    Choice("Left", DirectionalInput.LeftRelative),
                    Choice("Right", DirectionalInput.RightRelative)));

            AddBitGroup(inputsPanel, "Button Input", nameof(BCM_Entry.ButtonInput),
                new BcmChoiceGroup(string.Empty,
                    Choice("Lock On", ButtonInput.LockOn),
                    Choice("Descend", ButtonInput.Descend),
                    Choice("Dragon Radar", ButtonInput.DragonRadar),
                    Choice("Jump 2", ButtonInput.Jump_2),
                    Choice("Ultimate Menu", ButtonInput.UltimateMenu),
                    Choice("Skill Input", ButtonInput.SkillInput),
                    Choice("Super Menu Plus Skill Input", ButtonInput.SuperMenuPlusSkillInput),
                    Choice("Ultimate Menu Plus Skill Input", ButtonInput.UltimateMenuPlusSkillInput),
                    Choice("Unk20", ButtonInput.Unk20),
                    Choice("Ultimate Skill1", ButtonInput.UltimateSkill1),
                    Choice("Ultimate Skill2", ButtonInput.UltimateSkill2),
                    Choice("Awoken Skill", ButtonInput.AwokenSkill),
                    Choice("Evasive Skill", ButtonInput.EvasiveSkill),
                    Choice("Super Skill1", ButtonInput.SuperSkill1),
                    Choice("Super Skill2", ButtonInput.SuperSkill2),
                    Choice("Super Skill3", ButtonInput.SuperSkill3),
                    Choice("Super Skill4", ButtonInput.SuperSkill4),
                    Choice("Skill Menu", ButtonInput.SkillMenu),
                    Choice("Boost", ButtonInput.Boost),
                    Choice("Guard", ButtonInput.Guard),
                    Choice("Unk8", ButtonInput.Unk8),
                    Choice("Light", ButtonInput.Light),
                    Choice("Heavy", ButtonInput.Heavy),
                    Choice("Blast", ButtonInput.Blast),
                    Choice("Jump", ButtonInput.Jump),
                    Choice("Unk26", ButtonInput.Unk26),
                    Choice("Unk27", ButtonInput.Unk27),
                    Choice("Unk28", ButtonInput.Unk28),
                    Choice("Ultimate Menu 2", ButtonInput.UltimateMenu_2),
                    Choice("Unk30", ButtonInput.Unk30),
                    Choice("Unk31", ButtonInput.Unk31),
                    Choice("Unk32", ButtonInput.Unk32)));

            AddOptionGroup(inputsPanel, "Hold Down Conditions", nameof(BCM_Entry.HoldDownConditions),
                new BcmOptionGroup("Charge Type", 0x00030000, 16,
                    new BcmChoice("Automatic", 0),
                    new BcmChoice("Manual", 1),
                    new BcmChoice("Hold down to loop", 2),
                    new BcmChoice("Unknown (0x3)", 3)),
                new BcmOptionGroup("Options #2", 0x0000F000, 12,
                    new BcmChoice("Unknown (0x0)", 0),
                    new BcmChoice("Unknown (0x1)", 1),
                    new BcmChoice("Unknown (0x2)", 2),
                    new BcmChoice("Unknown (0x3)", 3)),
                new BcmOptionGroup("Options #3", 0x00000F00, 8,
                    new BcmChoice("Unknown (0x0)", 0),
                    new BcmChoice("Unknown (0x1)", 1),
                    new BcmChoice("Unknown (0x2)", 2),
                    new BcmChoice("Unknown (0x3)", 3)),
                new BcmOptionGroup("Options #4", 0x000000F0, 4,
                    new BcmChoice("Unknown (0x0)", 0),
                    new BcmChoice("Unknown (0x1)", 1),
                    new BcmChoice("Unknown (0x2)", 2),
                    new BcmChoice("Unknown (0x3)", 3)),
                new BcmOptionGroup("Behaviour", 0x0000000F, 0,
                    new BcmChoice("Continue until released", 0),
                    new BcmChoice("Delay until released", 1),
                    new BcmChoice("Unknown (0x2)", 2),
                    new BcmChoice("Stop skill from activating", 4)));
        }

        private void BuildActivatorTab()
        {
            AddOptionGroup(activatorPanel, "Mode", nameof(BCM_Entry.I_00),
                new BcmOptionGroup("Mode", 0xFFFF, 0,
                    new BcmChoice("None", 0),
                    new BcmChoice("Use Skill Upgrades", 1),
                    new BcmChoice("Opponent Reached Ground", 0x10),
                    new BcmChoice("Unknown (0x2)", 0x2),
                    new BcmChoice("Unknown (0x4)", 0x4),
                    new BcmChoice("Unknown (0x8)", 0x8)));

            if (SelectedEntry.I_00 == 1)
                AddUpgradeLevelEditor(activatorPanel);
            else
                AddOpponentSizeEditor(activatorPanel);

            AddEditor(activatorPanel, "Minimum Loop Duration", nameof(BCM_Entry.MinimumLoopDuration), EditorValueMode.Decimal);
            AddEditor(activatorPanel, "Maximum Loop Duration", nameof(BCM_Entry.MaximumLoopDuration), EditorValueMode.Decimal);

            Panel callbackFields = AddEditorGroup(activatorPanel, "BCM Callback");
            AddReceiverLinkEditor(callbackFields);

            Panel resourceFields = AddEditorGroup(activatorPanel, "Resource");
            AddEditor(resourceFields, "Ki Cost", nameof(BCM_Entry.I_64), EditorValueMode.Decimal);
            AddEditor(resourceFields, "Stamina Cost", nameof(BCM_Entry.StaminaCost), EditorValueMode.Decimal);
            AddEditor(resourceFields, "Ki Required", nameof(BCM_Entry.KiRequired), EditorValueMode.Decimal);
            AddEditor(resourceFields, "Health Required", nameof(BCM_Entry.HealthRequired), EditorValueMode.Decimal);

            Panel userStateFields = AddEditorGroup(activatorPanel, "User State");
            AddEditor(userStateFields, "CUS Aura", nameof(BCM_Entry.CusAura), EditorValueMode.Decimal);
            AddEditor(userStateFields, "Transformation Stage", nameof(BCM_Entry.TransStage), EditorValueMode.Decimal);

            AddBitGroup(activatorPanel, "Primary Conditions", nameof(BCM_Entry.PrimaryActivatorConditions),
                new BcmChoiceGroup("Opponent State",
                    Choice("Health < 25%", PrimaryActivatorConditions.TargetsHealthLessThan25),
                    Choice("In Knockback", PrimaryActivatorConditions.OpponentKnockback),
                    Choice("Being Targeted", PrimaryActivatorConditions.TargetingOpponent)),
                new BcmChoiceGroup("BAC Callback",
                    Choice("When an attack hits", PrimaryActivatorConditions.OnAttackHit),
                    Choice("Running BAC Entry attack Hits", PrimaryActivatorConditions.CurrentBacEntryHits),
                    new BcmChoice("Pass when Guarding", (uint)PrimaryActivatorConditions.AttackBlocked, "When this is active, the BCM condition \"Attack hit\" or \"Running BAC Entry Attack hit\" pass even when opponent is guarding."),
                    Choice("Active Projectile", PrimaryActivatorConditions.ActiveProjectile)),
                new BcmChoiceGroup("User State",
                    Choice("User's Health < 25% (One Use)", PrimaryActivatorConditions.UsersHealth_OneUse),
                    Choice("User's Health < 25%", PrimaryActivatorConditions.UsersHealth),
                    Choice("Transformed", PrimaryActivatorConditions.InTransformedState),
                    Choice("Base form", PrimaryActivatorConditions.InBaseForm),
                    Choice("Not Moving", PrimaryActivatorConditions.Idle),
                    Choice("Flash on/off unless targeting", PrimaryActivatorConditions.Unk10)),
                new BcmChoiceGroup("Position",
                    Choice("Standing", PrimaryActivatorConditions.Standing),
                    Choice("Floating", PrimaryActivatorConditions.Floating),
                    Choice("Touching \"ground\"", PrimaryActivatorConditions.TouchingGround),
                    Choice("Close to opponent", PrimaryActivatorConditions.CloseToTarget),
                    Choice("Far from opponent", PrimaryActivatorConditions.FarFromTarget),
                    Choice("Not near map ceiling", PrimaryActivatorConditions.NotNearStageCeiling),
                    Choice("Not near certain objects", PrimaryActivatorConditions.NotNearCertainObjects)),
                new BcmChoiceGroup("Resource",
                    Choice("Stamina > 0%", PrimaryActivatorConditions.StaminaAboveZero),
                    Choice("Pass when Stamina Reaches 0", PrimaryActivatorConditions.CounterProjectile),
                    Choice("Ki < 100%", PrimaryActivatorConditions.KiBelow100),
                    Choice("Ki > 0%", PrimaryActivatorConditions.KiAboveZero)),
                new BcmChoiceGroup("Counter",
                    Choice("Counter Melee", PrimaryActivatorConditions.Unk17),
                    Choice("Counter Projectiles", PrimaryActivatorConditions.Unk18),
                    Choice("Counter All", PrimaryActivatorConditions.CounterMelee)),
                new BcmChoiceGroup("Touching",
                    Choice("Ground", PrimaryActivatorConditions.Ground),
                    Choice("Opponent", PrimaryActivatorConditions.Opponent)),
                new BcmChoiceGroup("Unknown",
                    Choice("Unknown (0x4)", PrimaryActivatorConditions.Unk11),
                    Choice("Unknown (0x2)", PrimaryActivatorConditions.Unk22),
                    Choice("Unknown (0x8)", PrimaryActivatorConditions.Unk24)));

            AddBitGroup(activatorPanel, "Activator State", nameof(BCM_Entry.ActivatorState),
                new BcmChoiceGroup("State",
                    Choice("Receiving Damage", ActivatorState.ReceivingDamage),
                    Choice("Jumping", ActivatorState.Jumping),
                    Choice("Not being damaged", ActivatorState.NotReceivingDamage),
                    Choice("Target attacking player", ActivatorState.TargetIsAttacking)),
                new BcmChoiceGroup("Action",
                    Choice("Idle", ActivatorState.Idle),
                    Choice("Combo/skill", ActivatorState.Attacking),
                    Choice("Boosting", ActivatorState.Boosting),
                    Choice("Guarding", ActivatorState.Guarding)));
        }

        private void BuildBacTab()
        {
            AddEditor(bacPanel, "Primary BAC", nameof(BCM_Entry.BacEntryPrimary), EditorValueMode.Decimal);
            AddEditor(bacPanel, "Charge BAC", nameof(BCM_Entry.BacEntryCharge), EditorValueMode.Decimal);
            AddEditor(bacPanel, "User Connect BAC", nameof(BCM_Entry.BacEntryUserConnect), EditorValueMode.Decimal);
            AddEditor(bacPanel, "Victim Connect BAC", nameof(BCM_Entry.BacEntryVictimConnect), EditorValueMode.Decimal);
            AddEditor(bacPanel, "Airborne BAC", nameof(BCM_Entry.BacEntryAirborne), EditorValueMode.Decimal);
            AddEditor(bacPanel, "Targeting Override BAC", nameof(BCM_Entry.BacEntryTargetingOverride), EditorValueMode.Decimal);
            AddOptionGroup(bacPanel, "Mode", nameof(BCM_Entry.RandomFlag),
                new BcmOptionGroup("Mode", 0xFFFF, 0,
                    new BcmChoice("None", 0),
                    new BcmChoice("Random BAC Entry", 1),
                    new BcmChoice("No Target Correction", 2),
                    new BcmChoice("3 Instance Setup", 3),
                    new BcmChoice("Unknown (0x4)", 4),
                    new BcmChoice("Unknown (0x6)", 6)));
        }

        private void BuildMiscTab()
        {
            AddLinkEditor(miscPanel, "Sibling Idx", nameof(BCM_Entry.LoopAsSibling), SiblingIndex);
            AddLinkEditor(miscPanel, "Child Idx", nameof(BCM_Entry.LoopAsChild), ChildIndex);
        }

        private void BuildUnknownTab()
        {
            AddEditor(unknownPanel, "I_00", nameof(BCM_Entry.I_00), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_36", nameof(BCM_Entry.I_36), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_68", nameof(BCM_Entry.I_68), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_72", nameof(BCM_Entry.I_72), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_80", nameof(BCM_Entry.I_80), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_88", nameof(BCM_Entry.I_88), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_104", nameof(BCM_Entry.I_104), EditorValueMode.Hex);
            AddEditor(unknownPanel, "I_108", nameof(BCM_Entry.I_108), EditorValueMode.Hex);
        }

    }
}
