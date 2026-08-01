using easpace.Desktop.Constants;

namespace easpace.Desktop.Models.Activities;

public class RoutineDataEntry : DataEntry
{
    public RoutineState State { get; set; } = RoutineState.None;
}