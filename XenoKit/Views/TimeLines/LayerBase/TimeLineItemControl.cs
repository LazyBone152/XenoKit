using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using Xv2CoreLib;
using Xv2CoreLib.Resource.UndoRedo;
using System.ComponentModel;
using System;
using Xv2CoreLib.BAC;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Xv2CoreLib.Resource;

namespace XenoKit.Views.TimeLines
{
    public class TimeLineItemControl : Button, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        internal bool ReadyToDraw { get; set; } = true;

        public ITimeLineParent ParentTimeLine { get; private set; }

        //These two values are to allow the smooth dragging movement that wouldn't be possible with a pure integer start and duration 
        public float StartTimeFloat { get; private set; }
        public float DurationFloat { get; private set; }

        public bool UseGhostTrail { get; set; }

        private float NormalHeight { get; set; }
        private float MouseOverSize => 1.4f;

        #region DPs
        public int StartTime
        {
            get => (int)GetValue(StartTimeProperty);
            set => SetValue(StartTimeProperty, value);
        }

        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register(nameof(StartTime), typeof(int), typeof(TimeLineItemControl), new UIPropertyMetadata(-1, new PropertyChangedCallback(OnTimeValueChanged)));

        public int Duration
        {
            get => (int)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(int), typeof(TimeLineItemControl), new UIPropertyMetadata(1, new PropertyChangedCallback(OnTimeValueChanged)));

        public double EditBorderThreshold
        {
            get => (double)GetValue(EditBorderThresholdProperty);
            set => SetValue(EditBorderThresholdProperty, value);
        }

        public static readonly DependencyProperty EditBorderThresholdProperty = DependencyProperty.Register(nameof(EditBorderThreshold), typeof(double), typeof(TimeLineItemControl), new UIPropertyMetadata(4.0, null));

        private static void OnTimeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimeLineItemControl ctrl = d as TimeLineItemControl;
            if (ctrl != null)
                ctrl.PlaceOnCanvas();
        }

        #endregion

        public TimeLineItemControl(ITimeLineParent parent, bool useGhostTrail)
        {
            ParentTimeLine = parent;
            UseGhostTrail = useGhostTrail;
            Focusable = false;
        }

        #region Draw
        public float MainWidth { get; set; }
        public float TrailWidth { get; set; }

        private ContentPresenter _LeftIndicator;
        private ContentPresenter _RightIndicator;
        private ContentPresenter _MouseOver;

        public void SetFloatingStartAndDuration(float start, float duration)
        {
            StartTimeFloat = start;
            DurationFloat = duration;
        }

        internal void ResetStartAndDurationFromSource()
        {
            if(DataContext is ITimeLineItem item)
            {
                StartTime = item.TimeLine_StartTime;
                Duration = item.TimeLine_Duration;
            }
        }

        internal void PlaceOnCanvas()
        {
            UpdateWidth();

            double p = CalculateLeftPosition();
            if (p >= 0)
            {
                Canvas.SetLeft(this, (int)p);
            }
        }

        private void UpdateWidth()
        {
            double mainWidth = CalculateMainWidth();

            if (mainWidth > 0)
            {
                MainWidth = (int)mainWidth;
                NotifyPropertyChanged(nameof(MainWidth));
            }

            if (UseGhostTrail)
            {
                double trailWidth = CalculateTrailWidth();

                TrailWidth = (int)trailWidth;
                NotifyPropertyChanged(nameof(TrailWidth));

                Width = mainWidth + trailWidth;
            }
            else
            {
                Width = mainWidth;
            }
        }

        public void ApplyContentTemplate(DataTemplate template)
        {
            ContentTemplate = template;
        }

        public override void OnApplyTemplate()
        {
            _LeftIndicator = Template.FindName("PART_LeftIndicator", this) as ContentPresenter;
            _RightIndicator = Template.FindName("PART_RightIndicator", this) as ContentPresenter;
            _MouseOver = Template.FindName("PART_MouseOver", this) as ContentPresenter;
            if (_LeftIndicator != null)
                _LeftIndicator.Visibility = Visibility.Collapsed;
            if (_RightIndicator != null)
                _RightIndicator.Visibility = Visibility.Collapsed;
            if (_RightIndicator != null)
                _MouseOver.Visibility = Visibility.Collapsed;
            base.OnApplyTemplate();

        }

        internal double CalculateMainWidth()
        {
            try
            {
                return ConvertTimeToDistance(Duration);
            }
            catch
            {
                return 1;
            }
        }

        internal double CalculateTrailWidth()
        {
            try
            {
                return Math.Max(ConvertTimeToDistance(ParentTimeLine.GetGhostDuration(DataContext as ITimeLineItem)), 0);
            }
            catch
            {
                return 0;
            }
        }

        internal double CalculateLeftPosition()
        {
            return ConvertTimeToDistance(StartTime);
        }

        private void SetIndicators(Visibility left, Visibility right)
        {
            if (_LeftIndicator != null)
            {
                _LeftIndicator.Visibility = left;
            }
            if (_RightIndicator != null)
            {
                _RightIndicator.Visibility = right;
            }

            if(left == Visibility.Visible || right == Visibility.Visible)
            {
                _MouseOver.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region MouseEvents
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            _MouseOver.Visibility = Visibility.Visible;
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _MouseOver.Visibility = Visibility.Visible;
            //Show the resize indicators only if the timeline object is large enough
            if(ConvertTimeToDistance(Duration) >= 15)
            {
                switch (GetClickAction(false))
                {

                    case TimeLineAction.StretchStart:
                        SetIndicators(Visibility.Visible, Visibility.Collapsed);
                        break;
                    case TimeLineAction.StretchEnd:
                        SetIndicators(Visibility.Collapsed, Visibility.Visible);
                        //this.Cursor = Cursors.SizeWE;//Cursors.Hand;//Cursors.ScrollWE;
                        break;
                    default:
                        SetIndicators(Visibility.Collapsed, Visibility.Collapsed);
                        break;
                }
            }
            else
            {
                SetIndicators(Visibility.Collapsed, Visibility.Collapsed);
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _MouseOver.Visibility = Visibility.Collapsed;
            SetIndicators(Visibility.Collapsed, Visibility.Collapsed);
            base.OnMouseLeave(e);
        }

        #endregion

        #region Conversion Utilities
        private double ConvertTimeToDistance(float time)
        {
            return (double)(time * ParentTimeLine.UnitSize);
        }

        private float ConvertDistanceToTime(double distance)
        {
            return (float)(distance / ParentTimeLine.UnitSize);
        }

        #endregion

        #region Manipulation Tools
        internal TimeLineAction GetClickAction(bool checkVisibility)
        {
            double X = Mouse.GetPosition(this).X;
            double borderThreshold = (double)GetValue(EditBorderThresholdProperty);// 4;

            if (X < borderThreshold && (_LeftIndicator.Visibility == Visibility.Visible || !checkVisibility))
                return TimeLineAction.StretchStart;
            if (X > Width - borderThreshold && (_RightIndicator.Visibility == Visibility.Visible || !checkVisibility))
                return TimeLineAction.StretchEnd;
            return TimeLineAction.Move;

        }

        internal bool CanDelta(int StartOrEnd, double deltaX)
        {

            double unitS = ParentTimeLine.UnitSize;
            double threshold = unitS / 3.0;
            double newW = unitS;
            if (StartOrEnd == 0)//we are moving the start
            {
                if (deltaX < 0)
                    return true;
                //otherwises get what our new width would be
                newW = Width - deltaX;//delta is + but we are actually going to shrink our width by moving start +
                return newW > threshold;
            }
            else
            {
                if (deltaX > 0)
                    return true;
                newW = Width + deltaX;
                return newW > threshold;
            }
        }

        internal double GetDeltaTime(double deltaX)
        {
            return ConvertDistanceToTime(deltaX);
        }

        internal void GetPlacementInfo(ref double left, ref double width, ref double end)
        {
            left = Canvas.GetLeft(this);
            width = MainWidth;
            end = left + MainWidth;
            //width = Width;
            //end = left + Width;
            /*
            //Somewhere on the process of removing a timeline control from the visual tree
            //it resets our start time to min value.  In that case it then results in ridiculous placement numbers
            //that this feeds to the control and crashes the whole app in a strange way.
            if (TimeLineStartTime == DateTime.MinValue)
            {
                left = 0;
                width = 1;
                end = 1;
            }
            */
        }

        internal void UpdateFromSource()
        {
            if(DataContext is ITimeLineItem item)
            {
                StartTime = item.TimeLine_StartTime;
                Duration = item.TimeLine_Duration;
            }
        }

        internal void MoveMe(double deltaX, List<IUndoRedo> undos = null, int minFrame = -1, int maxFrame = int.MaxValue)
        {
            //double left = Xv2CoreLib.Resource.MathHelpers.Clamp(ConvertTimeToDistance(minFrame), ConvertTimeToDistance(maxFrame), Canvas.GetLeft(this) + ConvertTimeToDistance(StartTimeFloat) + deltaX);
            double newLeft = Canvas.GetLeft(this) + ConvertTimeToDistance(StartTimeFloat) + deltaX;
            double newStartTime = ConvertDistanceToTime(newLeft);
            double cappedEndFrame = Xv2CoreLib.Resource.MathHelpers.Clamp(minFrame + 1, maxFrame, newStartTime + Duration);
            double cappedStartFrame = cappedEndFrame - Duration;
            //double cappedStartFrame = newStartTime + Duration - cappedEndFrame;

            double left = ConvertTimeToDistance((float)cappedStartFrame);

            if (left < 0)
                left = 0;
            Canvas.SetLeft(this, ConvertTimeToDistance((int)ConvertDistanceToTime(left)));

            float fST = ConvertDistanceToTime(left);
            StartTime = (int)fST;
            StartTimeFloat = fST - StartTime;
            
            if(undos != null && DataContext is ITimeLineItem timelineItem)
            {
                timelineItem.UpdateSourceValues((ushort)StartTime, (ushort)Duration, undos);
            }
        }

        internal int MoveEndTime(double delta, List<IUndoRedo> undos = null)
        {
            //Sync duration with animation length if there is a mismatch
            //IDEALLY this wouldn't be needed, as the stretching should happen at the point of the actual duration, but i couldnt figure out how to move the indicators properly
            if (UseGhostTrail && TrailWidth > 0)
            {
                if(DataContext is BAC_Type0 anim)
                {
                    if (BAC_Type0.IsFullBodyAnimation(anim.EanType))
                    {
                        ushort newDuration = (ushort)((ushort)ParentTimeLine.GetGhostDuration(anim) + anim.Duration);
                        undos?.Add(new UndoablePropertyGeneric(nameof(anim.Duration), anim, anim.Duration, newDuration));
                        anim.Duration = newDuration;
                    }

                    Duration = anim.Duration;
                    UpdateWidth();
                }
            }

            int originalDuration = Duration;
            DurationFloat += ConvertDistanceToTime(delta);

            if (Duration <= 1f && delta < 0f)
                return 0;

            //Calculate current floating duration
            float fDuration = Duration + DurationFloat;
            int iDuration = (int)fDuration;
            DurationFloat = fDuration - iDuration;

            if (iDuration >= 1)
            {
                Width = ConvertTimeToDistance(iDuration);
                Duration = iDuration;
            }

            if (undos != null && DataContext is ITimeLineItem timelineItem)
            {
                timelineItem.UpdateSourceValues((ushort)StartTime, (ushort)Duration, undos);
            }

            return originalDuration - Duration;
        }

        internal int MoveStartTime(double delta, List<IUndoRedo> undos = null)
        {
            double curLeft = Canvas.GetLeft(this);
            if ((curLeft == 0 && delta < 0) || (Duration <= 1f && delta > 0))
                return 0;

            int start = (int)StartTime;
            MoveMe(delta);
            Duration = (int)Duration - (StartTime - start);
            Width = ConvertTimeToDistance(Duration);

            if (undos != null && DataContext is ITimeLineItem timelineItem)
            {
                timelineItem.UpdateSourceValues((ushort)StartTime, (ushort)Duration, undos);
            }

            return start - StartTime;
        }

        internal void ChangeStartTime(int startTime, List<IUndoRedo> undos = null)
        {
            if(DataContext is ITimeLineItem timelineItem)
            {
                timelineItem.UpdateSourceValues((ushort)startTime, (ushort)timelineItem.TimeLine_Duration, undos);
            }
            UpdateFromSource();
            PlaceOnCanvas();
        }

        internal void ChangeDuration(int duration, List<IUndoRedo> undos = null)
        {
            if (DataContext is ITimeLineItem timelineItem)
            {
                timelineItem.UpdateSourceValues((ushort)timelineItem.TimeLine_StartTime, (ushort)duration, undos);
            }
            UpdateFromSource();
            PlaceOnCanvas();
        }

        /// <summary>
        /// Sets up with a default of 55 of our current units in size.
        /// </summary>
        internal void InitializeDefaultLength()
        {
            Duration = (int)ConvertDistanceToTime(10 * ParentTimeLine.UnitSize);
            Width = CalculateTrailWidth();
        }
        #endregion

    }
}