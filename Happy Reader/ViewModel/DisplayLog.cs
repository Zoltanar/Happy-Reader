using System;
using System.Windows;
using System.Windows.Controls;
using Happy_Reader.Database;

namespace Happy_Reader.ViewModel
{
	public class DisplayLog
	{
		public DateTime Timestamp { get; }
		public UIElement Description { get; }

		public DisplayLog(Log log)
		{
			Timestamp = log.Timestamp;
			var textBlock = new TextBlock();
			foreach (var inline in log.Description) textBlock.Inlines.Add(inline);
			Description = textBlock;
		}
	}
}
