using System.Collections.Generic;
using easpace.Desktop.Models;

namespace easpace.Desktop.Constants;

// TODO: refactor this as default mood labels after setting up a db
public static class MoodLabelsData
{
    public static List<MoodLabel> GetMoodLabels()
    {
        return
        [
            new() { Name = "MOOD_LABEL_JOY" },
            new() { Name = "MOOD_LABEL_HOPE" },
            new() { Name = "MOOD_LABEL_AWE" },
            new() { Name = "MOOD_LABEL_GRATITUDE" },
            new() { Name = "MOOD_LABEL_COURAGE" },
            new() { Name = "MOOD_LABEL_CONFIDENCE" },
            new() { Name = "MOOD_LABEL_PEACE" },
            new() { Name = "MOOD_LABEL_CALM" },
            new() { Name = "MOOD_LABEL_RELIEF" },
            new() { Name = "MOOD_LABEL_CONTENTMENT" },
            new() { Name = "MOOD_LABEL_SADNESS" },
            new() { Name = "MOOD_LABEL_ANXIETY" },
            new() { Name = "MOOD_LABEL_ANGER" },
            new() { Name = "MOOD_LABEL_GUILT" },
            new() { Name = "MOOD_LABEL_LONELINESS" },
            new() { Name = "MOOD_LABEL_EXHAUSTION" },
            new() { Name = "MOOD_LABEL_FRUSTRATION" }
        ];
    }
}
