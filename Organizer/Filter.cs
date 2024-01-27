using System;

public static class Filter {
	public static bool NotCompleted(Task task) {
		return task.Completed == DateTime.MinValue;
	}
	
	public static bool Completed(Task task) {
		return task.Completed != DateTime.MinValue;
	}

	public static bool WithDate(Task task) {
		return task.Date != DateTime.MinValue;
	}

	public static bool NoTasksWithChildren(Task task) {
		return task.Children.Count < 1;
	}
	
	// Pseudo-filters

	public static bool NoRootTaskParent(Task task) {
		return true;
	}

	public static bool NoHierarchy(Task task) {
		return true;
	}
}