using System;
using Godot;

public partial class Filter : GodotObject {
	public Task Task;
	
	public bool NotCompleted() {
		return Task.Completed == DateTime.MinValue;
	}
	
	public bool Completed() {
		return Task.Completed != DateTime.MinValue;
	}
	
	public bool CompletedLastMonth() {
		return Task.Completed.AddMonths(1) >= DateTime.Now;
	}
	
	public bool CompletedLastYear() {
		return Task.Completed.AddYears(1) >= DateTime.Now;
	}

	public bool WithDate() {
		return Task.Date != DateTime.MinValue;
	}

	public bool NextMonth() {
		return Task.Date >= DateTime.Now && Task.Date <= DateTime.Now.AddMonths(1);
	}

	public bool NextYear() {
		return Task.Date >= DateTime.Now && Task.Date <= DateTime.Now.AddYears(1);
	}

	public bool NoTasksWithChildren() {
		return Task.Children.Count < 1;
	}
	
	// Pseudo-filters

	public bool NoRootTaskParent() {
		return true;
	}

	public bool NoHierarchy() {
		return true;
	}

	public bool WithPath() {
		return true;
	}
	
	public bool WithPathWithoutFirst() {
		return true;
	}

	public bool NoCompleteButton() {
		return true;
	}

	public bool WithProgressBar() {
		return true;
	}

	public bool Custom(string expressionString) {
		return Customizer.CalcExpression(Task, expressionString, this, false);
	}
}