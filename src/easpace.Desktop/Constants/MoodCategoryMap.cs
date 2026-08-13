// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;

namespace easpace.Desktop.Constants;

public static class MoodCategoryMap
{
    public static readonly Dictionary<MoodLabelState, HashSet<MoodState>> Categories = new()
    {
        // SlightlyPleasant or VeryPleasant
        { MoodLabelState.Happiness, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Joy, [MoodState.VeryPleasant] },
        { MoodLabelState.Cheerfulness, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Satisfaction, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Pride, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Confidence, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Courage, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Passion, [MoodState.VeryPleasant] },
        { MoodLabelState.Excitement, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Hope, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Gratitude, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Carefreeness, [MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Relief, [MoodState.Neutral, MoodState.SlightlyPleasant] },
        
        // Neutral or SlightPleasant or VeryPleasant
        { MoodLabelState.Peace, [MoodState.Neutral, MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Calmness, [MoodState.Neutral, MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        { MoodLabelState.Indifference, [MoodState.Neutral] },
        { MoodLabelState.Surprise, [MoodState.SlightlyUnpleasant, MoodState.Neutral, MoodState.SlightlyPleasant] },
        { MoodLabelState.Amazement, [MoodState.Neutral, MoodState.SlightlyPleasant, MoodState.VeryPleasant] },
        
        // SlightlyUnpleasant or VeryUnpleasant
        { MoodLabelState.Annoyance, [MoodState.SlightlyUnpleasant] },
        { MoodLabelState.Nervousness, [MoodState.SlightlyUnpleasant] },
        { MoodLabelState.Embarrassment, [MoodState.SlightlyUnpleasant] },
        { MoodLabelState.Jealousy, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Guilt, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Disappointment, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Worry, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Anxiety, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Stress, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Fear, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Anger, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Frustration, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Sadness, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Discouragement, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Exhaustion, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Loneliness, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        { MoodLabelState.Overwhelm, [MoodState.SlightlyUnpleasant, MoodState.VeryUnpleasant] },
        
        // VeryUnpleasant
        { MoodLabelState.Despair, [MoodState.VeryUnpleasant] },
        { MoodLabelState.Shame, [MoodState.VeryUnpleasant] },
        { MoodLabelState.Disgust, [MoodState.VeryUnpleasant] }
    };
    
    public static bool BelongsTo(this MoodLabelState label, MoodState state)
    {
        return Categories.TryGetValue(label, out var states) && states.Contains(state);
    }
}