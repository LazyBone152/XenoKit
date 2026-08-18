using Xv2CoreLib.BSA;

namespace XenoKit.ViewModel.BSA
{
    public class BsaType12ViewModel : BsaTypeBaseViewModel
    {
        private readonly BSA_Type12 type;

        public EepkType SkillType
        {
            get => type.EepkType;
            set => SetValue(nameof(type.EepkType), type.EepkType, value, v => type.EepkType = v, "BSA Send Projectile Signal Skill Type");
        }

        public int SkillID
        {
            get => type.SkillID;
            set => SetValue(nameof(type.SkillID), type.SkillID, value, v => type.SkillID = v, "BSA Type12 Skill ID");
        }

        public float SignalValue
        {
            get => type.F_00;
            set => SetValue(nameof(type.F_00), type.F_00, value, v => type.F_00 = v, "BSA Send Projectile Signal Value");
        }

        public int DeliveryMode
        {
            get => type.I_12;
            set => SetValue(nameof(type.I_12), type.I_12, value, v => type.I_12 = v, "BSA Send Projectile Signal Delivery Mode");
        }

        public float PauseRecipientTimeline
        {
            get => type.F_16;
            set => SetValue(nameof(type.F_16), type.F_16, value, v => type.F_16 = v, "BSA Send Projectile Signal Pause Recipient Timeline");
        }

        public BsaType12ViewModel(BSA_Type12 type) : base(type)
        {
            this.type = type;
        }

        protected override void UpdateProperties()
        {
            base.UpdateProperties();
            RaisePropertyChanged(() => SkillType);
            RaisePropertyChanged(() => SkillID);
            RaisePropertyChanged(() => SignalValue);
            RaisePropertyChanged(() => DeliveryMode);
            RaisePropertyChanged(() => PauseRecipientTimeline);
        }
    }
}
