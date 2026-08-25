// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;

namespace easpace.Desktop.Behaviors;

/// <summary>
/// Provides an attached behavior for the TextBlock control that animates the text change
/// by fading out the old text and fading in the new one.
/// </summary>
internal class AnimatedTextBehavior : AvaloniaObject
{
    /// <summary>
    /// Identifies the AnimatedText attached property.
    /// Bind the text to this property instead of TextBlock.Text to enable the animation.
    /// </summary>
    public static readonly AttachedProperty<string?> AnimatedTextProperty =
        AvaloniaProperty.RegisterAttached<AnimatedTextBehavior, TextBlock, string?>(
            "AnimatedText");
    
    // private cancellation token so we can cancel the previous animation if new data comes
    private static readonly AttachedProperty<CancellationTokenSource?> CtsProperty =
        AvaloniaProperty.RegisterAttached<AnimatedTextBehavior, TextBlock, CancellationTokenSource?>(
            "Cts");

    /// <summary>
    /// Gets the value of the AnimatedText property.
    /// </summary>
    public static string? GetAnimatedText(TextBlock element) => element.GetValue(AnimatedTextProperty);

    /// <summary>
    /// Sets the value of the AnimatedText property.
    /// </summary>
    public static void SetAnimatedText(TextBlock element, string? value) =>
        element.SetValue(AnimatedTextProperty, value);

    /// <summary>
    /// Initializes static members of the <see cref="AnimatedTextBehavior"/> class.
    /// </summary>
    static AnimatedTextBehavior()
    {
        AnimatedTextProperty.Changed.AddClassHandler<TextBlock>(OnAnimatedTextChanged);
    }

    /// <summary>
    /// Handles the change of the AnimatedText attached property.
    /// </summary>
    private static void OnAnimatedTextChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        var newText = e.NewValue as string;

        _ = UpdateTextAnimatedAsync(textBlock, newText);
    }

    /// <summary>
    /// Animates the transition between the old text and the new text.
    /// </summary>
    private static async Task UpdateTextAnimatedAsync(TextBlock textBlock, string? newText)
    {
        var cts = textBlock.GetValue(CtsProperty);
        cts?.Cancel();
        cts?.Dispose();

        cts = new CancellationTokenSource();
        textBlock.SetValue(CtsProperty, cts);

        var token = cts.Token;

        try
        {
            if (!textBlock.IsLoaded || textBlock.Opacity == 0)
            {
                textBlock.Text = newText;
                textBlock.Opacity = 1.0;
                return;
            }

            if (!string.IsNullOrEmpty(textBlock.Text))
            {
                var fadeOut = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(100),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters = { new Setter(Visual.OpacityProperty, textBlock.Opacity) }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                        }
                    }
                };
                await fadeOut.RunAsync(textBlock, token);
            }

            token.ThrowIfCancellationRequested();

            textBlock.Text = newText;

            var fadeIn = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(150),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                    }
                }
            };
            await fadeIn.RunAsync(textBlock, token);

            textBlock.Opacity = 1.0;
        }
        catch (OperationCanceledException)
        {
            // animation is canceled
        }
    }
}