using easpace.Desktop.Constants.Keys;

namespace easpace.Desktop.Models.Growth;

public class RoutineDataEntry : DataEntry
{
    public RoutineState State { get; set; } = RoutineState.None;
}