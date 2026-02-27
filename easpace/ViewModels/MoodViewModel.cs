using System;
using System.Collections.ObjectModel;
using easpace.Constants;
using easpace.Models;

namespace easpace.ViewModels;

public partial class MoodViewModel : PageViewModel
{
    public ObservableCollection<MoodEntry> MoodEntries { get; set; }
    public ObservableCollection<MoodLabel> MoodLabels { get; set; }

    public MoodViewModel()
    {
        Page = ApplicationPage.Mood;

        MoodEntries =
        [
            new MoodEntry
            {
                Date = DateTime.Now, MoodSliderValue = 0.9, Description = "Fantasztikus nap, minden sikerült!",
                Labels = ["siker", "boldogság"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-1), MoodSliderValue = 0.4,
                Description = "Kicsit fáradtnak érzem magam ma.", Labels = []
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-2), MoodSliderValue = 0.7,
                Labels = ["munka", "flow"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-3), MoodSliderValue = 0.2,
                Labels = ["időjárás"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-4), MoodSliderValue = 0.8, Description = "Egy jó edzés után minden jobb.",
                Labels = ["sport", "egészség"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-5), MoodSliderValue = 0.5, Description = "Átlagos szerda, semmi különös.",
                Labels = ["rutin"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-6), MoodSliderValue = 1.0, Description = "Végre hétvége és kirándulás!",
                Labels = ["természet", "kikapcsolódás"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-7), MoodSliderValue = 0.3,
                Labels = []
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-8), MoodSliderValue = 0.6, Description = "Olvasással töltöttem az estét.",
                Labels = ["könyv", "nyugalom"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-9), MoodSliderValue = 0.1,
                Description = "Rossz hír érkezett, mélyponton vagyok.", Labels = ["szomorúság"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-10), MoodSliderValue = 0.75, Description = "Régi barátokkal találkoztam.",
                Labels = ["barátok", "nosztalgia"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-11), MoodSliderValue = 0.45,
                Labels = ["türelem"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-12), MoodSliderValue = 0.85,
                Labels = ["tanulás", "fejlődés"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-13), MoodSliderValue = 0.55,
                Description = "Kicsit sokat agyaltam a jövőn.", Labels = ["gondolatok"]
            },

            new MoodEntry
            {
                Date = DateTime.Now.AddDays(-14), MoodSliderValue = 0.95,
                Description = "A kedvenc ételemet ettem vacsorára.", Labels = ["gasztro", "öröm"]
            }
        ];

        MoodLabels = new ObservableCollection<MoodLabel>(MoodLabelsData.GetMoodLabels());
    }
}
