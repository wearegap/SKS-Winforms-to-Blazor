#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Levels table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Levels
{
    public string Level { get; set; } = string.Empty;
}
