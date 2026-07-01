namespace easpace.Desktop.Models.Growth;

public abstract class NumericActivity : GrowthActivity<NumericDataEntry>
{
    public double? TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}