using System;
using Godot;

public partial class Group : GodotObject {
	public Task Task;

	public string ByDate() {
		return DateToString(Task.Date);
	}

	public string ByCompleted() {
		return DateToString(Task.Completed);
	}

	public string ByPriority() {
		return Task.Priority.ToString();
	}

	public string ByRootTask() {
		var parent = Task;
		while (App.Tasks.ContainsKey(parent.Parent)) {
			parent = App.Tasks[parent.Parent];
		}
		return parent.Text;
	}

	public string Custom(string expressionString) {
		return Customizer.CalcExpression(Task, expressionString, this, "");
	}

	private static string ToDate(int time) {
		var dateTime = Customizer.Epoch.AddSeconds(time).ToLocalTime();
		return DateToString(dateTime);
	}

	private static string DateToString(DateTime dateTime) {
		return $"{dateTime.ToLocalTime():dd.MM.y}";
	}
}
