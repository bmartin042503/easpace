using System;

namespace easpace.Desktop.Models.Activities;

public class DataEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
}