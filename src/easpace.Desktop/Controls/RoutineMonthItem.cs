// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.Controls;

public class RoutineMonthItem : Control
{
    #region Fields

    private readonly FontFamily _fontFamily = new("avares://easpace.Desktop/Assets/Fonts#Poppins");

    #endregion

    #region Styled & Direct Properties

    public static readonly DirectProperty<RoutineMonthItem, RoutineMonth?> MonthDataProperty =
        AvaloniaProperty.RegisterDirect<RoutineMonthItem, RoutineMonth?>(
            nameof(MonthData),
            o => o.MonthData,
            (o, v) => o.MonthData = v
        );

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<RoutineMonthItem, double>(nameof(ItemSpacing), 8.0);
    
    public static readonly StyledProperty<double> TitleSpacingProperty =
        AvaloniaProperty.Register<RoutineMonthItem, double>(nameof(TitleSpacing), 16.0);

    public static readonly StyledProperty<CornerRadius> ItemCornerRadiusProperty =
        AvaloniaProperty.Register<RoutineMonthItem, CornerRadius>(nameof(ItemCornerRadius), new CornerRadius(4));

    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<RoutineMonthItem, double>(nameof(BorderThickness), 1.0);

    public static readonly StyledProperty<IBrush?> TitleForegroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(TitleForeground));

    public static readonly StyledProperty<double> TitleFontSizeProperty =
        AvaloniaProperty.Register<RoutineMonthItem, double>(nameof(TitleFontSize), 16.0);

    public static readonly StyledProperty<IBrush?> DummyBackgroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(DummyBackground));

    public static readonly StyledProperty<IBrush?> DummyBorderBrushProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(DummyBorderBrush));

    public static readonly StyledProperty<IBrush?> NoneBackgroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(NoneBackground));

    public static readonly StyledProperty<IBrush?> NoneBorderBrushProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(NoneBorderBrush));

    public static readonly StyledProperty<IBrush?> CompletedBackgroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(CompletedBackground));

    public static readonly StyledProperty<IBrush?> CompletedBorderBrushProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(CompletedBorderBrush));

    public static readonly StyledProperty<IBrush?> NotCompletedBackgroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(NotCompletedBackground));

    public static readonly StyledProperty<IBrush?> NotCompletedBorderBrushProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(NotCompletedBorderBrush));

    public static readonly StyledProperty<IBrush?> TodayBorderBrushProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(TodayBorderBrush));

    public static readonly StyledProperty<IBrush?> NoneTextForegroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(NoneTextForeground));

    public static readonly StyledProperty<IBrush?> CompletedTextForegroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(CompletedTextForeground));

    public static readonly StyledProperty<IBrush?> NotCompletedTextForegroundProperty =
        AvaloniaProperty.Register<RoutineMonthItem, IBrush?>(nameof(NotCompletedTextForeground));

    public static readonly StyledProperty<double> DayFontSizeProperty =
        AvaloniaProperty.Register<RoutineMonthItem, double>(nameof(DayFontSize), 12.0);

    #endregion

    #region Properties

    public RoutineMonth? MonthData
    {
        get;
        set
        {
            if (SetAndRaise(MonthDataProperty, ref field, value)) InvalidateVisual();
        }
    }
    
    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public double TitleSpacing
    {
        get => GetValue(TitleSpacingProperty);
        set => SetValue(TitleSpacingProperty, value);
    }

    public CornerRadius ItemCornerRadius
    {
        get => GetValue(ItemCornerRadiusProperty);
        set => SetValue(ItemCornerRadiusProperty, value);
    }

    public double BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public IBrush? TitleForeground
    {
        get => GetValue(TitleForegroundProperty);
        set => SetValue(TitleForegroundProperty, value);
    }

    public double TitleFontSize
    {
        get => GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public IBrush? DummyBackground
    {
        get => GetValue(DummyBackgroundProperty);
        set => SetValue(DummyBackgroundProperty, value);
    }

    public IBrush? DummyBorderBrush
    {
        get => GetValue(DummyBorderBrushProperty);
        set => SetValue(DummyBorderBrushProperty, value);
    }

    public IBrush? NoneBackground
    {
        get => GetValue(NoneBackgroundProperty);
        set => SetValue(NoneBackgroundProperty, value);
    }

    public IBrush? NoneBorderBrush
    {
        get => GetValue(NoneBorderBrushProperty);
        set => SetValue(NoneBorderBrushProperty, value);
    }

    public IBrush? CompletedBackground
    {
        get => GetValue(CompletedBackgroundProperty);
        set => SetValue(CompletedBackgroundProperty, value);
    }

    public IBrush? CompletedBorderBrush
    {
        get => GetValue(CompletedBorderBrushProperty);
        set => SetValue(CompletedBorderBrushProperty, value);
    }

    public IBrush? NotCompletedBackground
    {
        get => GetValue(NotCompletedBackgroundProperty);
        set => SetValue(NotCompletedBackgroundProperty, value);
    }

    public IBrush? NotCompletedBorderBrush
    {
        get => GetValue(NotCompletedBorderBrushProperty);
        set => SetValue(NotCompletedBorderBrushProperty, value);
    }

    public IBrush? TodayBorderBrush
    {
        get => GetValue(TodayBorderBrushProperty);
        set => SetValue(TodayBorderBrushProperty, value);
    }

    public IBrush? NoneTextForeground
    {
        get => GetValue(NoneTextForegroundProperty);
        set => SetValue(NoneTextForegroundProperty, value);
    }

    public IBrush? CompletedTextForeground
    {
        get => GetValue(CompletedTextForegroundProperty);
        set => SetValue(CompletedTextForegroundProperty, value);
    }

    public IBrush? NotCompletedTextForeground
    {
        get => GetValue(NotCompletedTextForegroundProperty);
        set => SetValue(NotCompletedTextForegroundProperty, value);
    }

    public double DayFontSize
    {
        get => GetValue(DayFontSizeProperty);
        set => SetValue(DayFontSizeProperty, value);
    }

    #endregion

    #region Initialization

    static RoutineMonthItem()
    {
        AffectsRender<RoutineMonthItem>(
            MonthDataProperty,
            DummyBackgroundProperty,
            DummyBorderBrushProperty,
            NoneBackgroundProperty,
            NoneBorderBrushProperty,
            NoneTextForegroundProperty,
            CompletedBackgroundProperty,
            CompletedBorderBrushProperty,
            CompletedTextForegroundProperty,
            NotCompletedBackgroundProperty,
            NotCompletedBorderBrushProperty,
            NotCompletedTextForegroundProperty,
            TodayBorderBrushProperty,
            TitleForegroundProperty,
            BorderThicknessProperty,
            ItemCornerRadiusProperty,
            DayFontSizeProperty
        );

        AffectsMeasure<RoutineMonthItem>(
            MonthDataProperty,
            ItemSpacingProperty,
            TitleFontSizeProperty
        );
    }

    #endregion

    #region Layout & Rendering

    protected override Size MeasureOverride(Size availableSize)
    {
        if (MonthData == null) return new Size(0, 0);

        var daysInMonth = DateTime.DaysInMonth(MonthData.Year, MonthData.Month);
        var firstDay = new DateTime(MonthData.Year, MonthData.Month, 1);

        // standard european calendar (monday = 0, sunday = 6)
        var startOffset = (int)firstDay.DayOfWeek - 1;
        if (startOffset < 0) startOffset = 6;

        var totalCells = startOffset + daysInMonth;
        var rows = (int)Math.Ceiling(totalCells / 7.0);

        // if there's no width constraint (e.g. inside an unconstrained horizontal panel), provide a reasonable default
        var width = double.IsInfinity(availableSize.Width) ? 350 : availableSize.Width;

        // calculate cell width to determine a fallback height (maintaining aspect ratio)
        var cellWidth = Math.Max(0, (width - 6 * ItemSpacing) / 7.0);

        // calculate an ideal height based on width (60% of width)
        var cellHeight = cellWidth * 0.6;

        // adding space for the title (font size + some margin)
        var titleSpace = TitleFontSize + TitleSpacing;

        // determine the final height: if we have constrained height use it, else calculate from ideal item height
        var height = availableSize.Height;
        if (double.IsInfinity(height))
        {
            height = titleSpace + rows * cellHeight + (rows - 1) * ItemSpacing;
        }

        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        if (MonthData == null) return;

        var typeface = new Typeface(_fontFamily);

        var date = new DateTime(MonthData.Year, MonthData.Month, 1);
        
        var culture = CultureInfo.CurrentUICulture;
        var titleString = date.ToString("Y", culture);
        
        var titleText = new FormattedText(
            culture.TextInfo.ToTitleCase(titleString),
            culture,
            FlowDirection.LeftToRight,
            typeface,
            TitleFontSize,
            TitleForeground ?? Brushes.Black);

        // draw title (month name)
        context.DrawText(titleText, new Point(0, 0));

        var titleSpace = TitleFontSize + TitleSpacing;
        var daysInMonth = DateTime.DaysInMonth(MonthData.Year, MonthData.Month);

        // standard european calendar (monday = 0, sunday = 6)
        var startOffset = (int)date.DayOfWeek - 1;
        if (startOffset < 0) startOffset = 6;

        var totalCells = startOffset + daysInMonth;
        var rows = (int)Math.Ceiling(totalCells / 7.0);

        // calculate dynamic item width based on actual control width and spacing
        var availableWidth = Bounds.Width;
        var cellWidth = Math.Max(0, (availableWidth - 6 * ItemSpacing) / 7.0);

        // calculate an ideal height based on width (60% of width)
        var cellHeight = cellWidth * 0.6;

        // create pens for borders
        var dummyPen = new Pen(DummyBorderBrush, BorderThickness);
        var nonePen = new Pen(NoneBorderBrush, BorderThickness);
        var completedPen = new Pen(CompletedBorderBrush, BorderThickness);
        var notCompletedPen = new Pen(NotCompletedBorderBrush, BorderThickness);
        var todayPen = new Pen(TodayBorderBrush, BorderThickness * 2); // thicker border for today

        var today = DateTime.Today;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < 7; col++)
            {
                var cellIndex = row * 7 + col;
                var x = col * (cellWidth + ItemSpacing);
                var y = titleSpace + row * (cellHeight + ItemSpacing);
                var rect = new Rect(x, y, cellWidth, cellHeight);

                // calculate corner radius for drawing
                var rx = (float)ItemCornerRadius.TopLeft;
                var ry = (float)ItemCornerRadius.TopLeft;

                // check if cell is a dummy or a real day
                if (cellIndex < startOffset || cellIndex >= startOffset + daysInMonth)
                {
                    // draw dummy
                    context.DrawRectangle(DummyBackground, dummyPen, rect, rx, ry);
                }
                else
                {
                    // it's a valid day
                    var dayNumber = cellIndex - startOffset + 1;
                    var currentDate = new DateTime(MonthData.Year, MonthData.Month, dayNumber);

                    // find entry for this day if it exists
                    var entry = MonthData.Entries?.FirstOrDefault(e => e.Timestamp.Date == currentDate.Date);
                    var state = entry?.State ?? RoutineState.None;

                    var bgBrush = NoneBackground;
                    var borderPen = nonePen;
                    var textBrush = NoneTextForeground;

                    switch (state)
                    {
                        // apply correct brushes based on routine state
                        case RoutineState.Completed:
                            bgBrush = CompletedBackground;
                            borderPen = completedPen;
                            textBrush = CompletedTextForeground;
                            break;
                        case RoutineState.NotCompleted:
                            bgBrush = NotCompletedBackground;
                            borderPen = notCompletedPen;
                            textBrush = NotCompletedTextForeground;
                            break;
                    }

                    // override border if it's today
                    if (currentDate.Date == today.Date)
                    {
                        borderPen = todayPen;
                    }

                    // draw day box
                    context.DrawRectangle(bgBrush, borderPen, rect, rx, ry);

                    // draw day number text
                    var dayText = new FormattedText(
                        dayNumber.ToString(),
                        culture,
                        FlowDirection.LeftToRight,
                        typeface,
                        DayFontSize,
                        textBrush ?? Brushes.Black);

                    // align text to top-right with a little padding
                    var textX = x + cellWidth - dayText.Width - 6;
                    var textY = y + 4;

                    context.DrawText(dayText, new Point(textX, textY));
                }
            }
        }
    }

    #endregion
}