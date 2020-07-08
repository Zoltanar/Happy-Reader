using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
			Description = new RichTextBox(new FlowDocument(log.Description))
			{
				Margin = new Thickness(0d), 
				Padding = new Thickness(0d), 
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				IsReadOnly = true,
			};
		}
	}
}
