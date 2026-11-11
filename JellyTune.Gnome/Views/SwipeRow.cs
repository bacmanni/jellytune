using System.Diagnostics;
using GLib;
using GObject;
using Gtk;
using JellyTune.Gnome.Enums;

namespace JellyTune.Gnome.Views;

[Subclass<ListBoxRow>(qualifiedName: "JellyTuneSwipeRow")]
[Template<AssemblyResource>("JellyTune.Gnome.Blueprints.swipe_row.ui")]
public partial class SwipeRow
{
    [Connect] private Overlay _overlay;
    [Connect] private Box _foregroundBox;
    [Connect] private Box _foregroundContent;
    [Connect] private Box _leftBox;
    [Connect] private Box _rightBox;
    [Connect] private Label _rightLabel;
    [Connect] private Label _leftLabel;

    private string? _leftStyle;
    private string? _rightStyle;
    
    private TrackRow _trackRow;
    private GestureDrag _gesture;

    private double _offset;
    private bool _directionLocked;
    private bool _horizontalSwipe;

    private uint _animationSource;

    private const double SwipeThreshold = 120;
    private const double MaxSwipeDistance = 160;
    private const double GestureThreshold = 8;

    private const int AnimationDuration = 200;

    public event Action<TrackRow>? OnSwipedRight;
    public event Action<TrackRow>? OnSwipedLeft;

    public TrackRow TrackRow => _trackRow;
    
    public static SwipeRow NewWithTrackRow(TrackRow trackRow, Dictionary<SwipeProperty, object> properties)
    {
        var obj = NewWithProperties([]);
        obj._trackRow = trackRow;

        trackRow.AddCssClass("swipe-foreground");
        obj._foregroundContent.Append(trackRow);

        if (properties.ContainsKey(SwipeProperty.RightTitle))
            obj._rightLabel.SetText(properties[SwipeProperty.RightTitle].ToString());

        if (properties.ContainsKey(SwipeProperty.LeftTitle))
            obj._leftLabel.SetText(properties[SwipeProperty.LeftTitle].ToString());
        
        if (properties.ContainsKey(SwipeProperty.RightStyle))
            obj._rightStyle = properties[SwipeProperty.RightStyle].ToString();
        
        if (properties.ContainsKey(SwipeProperty.LeftStyle))
            obj._leftStyle = properties[SwipeProperty.LeftStyle].ToString();
        
        obj.InitializeSwipe();
        return obj;
    }

    private void InitializeSwipe()
    {
        _leftBox.SetOpacity(0);
        _rightBox.SetOpacity(0);
        
        _gesture = GestureDrag.New();
        _gesture.SetButton(1);

        // Capture before the TrackRow handles the click.
        _gesture.SetPropagationPhase(PropagationPhase.Capture);

        _gesture.OnDragBegin += (_, args) =>
        {
            StopAnimation();
            _offset = 0;
            _directionLocked = false;
            _horizontalSwipe = false;
        };

        _gesture.OnDragUpdate += (_, args) =>
        {
            var x = args.OffsetX;
            var y = args.OffsetY;

            if (!_directionLocked)
            {
                if (Math.Abs(x) < GestureThreshold &&
                    Math.Abs(y) < GestureThreshold)
                    return;

                _directionLocked = true;
                _horizontalSwipe = Math.Abs(x) > Math.Abs(y);

                _gesture.SetState( _horizontalSwipe ? EventSequenceState.Claimed : EventSequenceState.Denied);
            }

            if (!_horizontalSwipe)
                return;

            _offset = Math.Clamp(
                x,
                -MaxSwipeDistance,
                MaxSwipeDistance);

            UpdateSwipeVisuals();
        };

        _gesture.OnDragEnd += (_, args) =>
        {
            if (!_horizontalSwipe)
                return;

            if (_offset >= SwipeThreshold)
                OnSwipeRight();
            else if (_offset <= -SwipeThreshold)
                OnSwipeLeft();

            ResetSwipe();
        };

        AddController(_gesture);
    }

    private void UpdateSwipeVisuals()
    {
        var progress = Math.Clamp(
            Math.Abs(_offset) / SwipeThreshold,
            0,
            1);

        var right = _offset > 0;

        // Move the foreground using margins.
        var distance = (int)Math.Abs(_offset);

        _trackRow.SetMarginStart(right ? distance : distance * -1);
        _trackRow.SetMarginEnd(right ? distance * -1 : distance);

        // Fade foreground.
        _trackRow.SetOpacity(1.0 - progress);

        // Background.
        RemoveCssClass(_leftStyle);
        RemoveCssClass(_rightStyle);

        if (progress > 0)
        {
            AddCssClass(right ? _leftStyle : _rightStyle);
        }
        
        _leftBox.SetOpacity(right ? progress: 0);
        _rightBox.SetOpacity(right ? 0 : progress);
    }


    private void OnSwipeRight()
    {
        OnSwipedRight?.Invoke(_trackRow);
    }

    private void OnSwipeLeft()
    {
        OnSwipedLeft?.Invoke(_trackRow);
    }

    private void ResetSwipe()
    {
        AnimateBack();
    }

    private void AnimateBack()
    {
        StopAnimation();

        var from = _offset;

        if (Math.Abs(from) < 0.5)
        {
            ApplyOffset(0);
            return;
        }

        var start = Stopwatch.GetTimestamp();

        _animationSource = GLib.Functions.TimeoutAdd(
            16,
            GLib.Constants.PRIORITY_DEFAULT,
            () =>
            {
                var elapsed =
                    (Stopwatch.GetTimestamp() - start) *
                    1000.0 / Stopwatch.Frequency;

                var t = Math.Clamp(
                    elapsed / AnimationDuration, 0, 1);

                // Ease-out cubic.
                var eased = 1 - Math.Pow(1 - t, 3);

                ApplyOffset(from * (1 - eased));

                if (t >= 1)
                {
                    _animationSource = 0;
                    ApplyOffset(0);
                    return false;
                }

                return true;
            });
    }

    private void ApplyOffset(double offset)
    {
        _offset = offset;
        UpdateSwipeVisuals();
    }

    private void StopAnimation()
    {
        if (_animationSource == 0)
            return;

        Source.Remove(_animationSource);
        _animationSource = 0;
    }

    public override void Dispose()
    {
        StopAnimation();
        base.Dispose();
    }
}
