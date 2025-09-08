using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eto.ExtendedRichTextArea;

public class FixedTabStops : TabStopsType
{
	public float Interval { get; set; }

	internal const float DefaultTabStop = 36; // 36 points, .5 inches

	public FixedTabStops()
	{
		Interval = DefaultTabStop;
	}

	public FixedTabStops(float interval)
	{
		if (interval <= 0)
			throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
		Interval = interval;
	}

	public override float GetNextTabStop(float x)
	{
		return x + Interval - (int)x % Interval;
	}
}

public class CustomTabStops : TabStopsType
{
	public List<float> TabStops { get; } = new();

	public float DefaultInterval { get; set; } = FixedTabStops.DefaultTabStop;

	public CustomTabStops()
	{
	}

	public CustomTabStops(params IEnumerable<float> tabStops)
	{
		TabStops.AddRange(tabStops);
		TabStops.Sort();
	}

	public override float GetNextTabStop(float x)
	{
		foreach (var tab in TabStops)
		{
			if (tab > x)
				return tab;
		}
		return x + DefaultInterval - (int)x % DefaultInterval;
	}
}

public abstract class TabStopsType
{
	public static TabStopsType Fixed(float interval) => new FixedTabStops(interval);

	public static TabStopsType Custom(params IEnumerable<float> tabStops)
	{
		var custom = new CustomTabStops();
		custom.TabStops.AddRange(tabStops);
		custom.TabStops.Sort();
		return custom;
	}

	public abstract float GetNextTabStop(float x);
}