using System;

public static class Filter {
	public static bool NotCompleted(Task task) {
		return task.Completed == DateTime.MinValue;
	}
	
	public static bool Completed(Task task) {
		return task.Completed != DateTime.MinValue;
	}
	
	public static bool CompletedLastDay(Task task) {
		return (DateTime.Now - task.Completed).TotalDays <= 1;
	}

	public static bool CompletedLastWeek(Task task) {
		return (DateTime.Now - task.Completed).TotalDays <= 7;
	}
	
	public static bool CompletedLastMonth(Task task) {
		return task.Completed.AddMonths(1) >= DateTime.Now;
	}
	
	public static bool CompletedLastYear(Task task) {
		return task.Completed.AddYears(1) >= DateTime.Now;
	}

	public static bool WithDate(Task task) {
		return task.Date != DateTime.MinValue;
	}

	public static bool NoTasksWithChildren(Task task) {
		return task.Children.Count < 1;
	}

	public static bool WithTagTermin(Task task) {
		return task.Tags?.Contains("termin") ?? false;
	}
	
	public static bool WithTagPlan(Task task) {
		return task.Tags?.Contains("plan") ?? false;
	}
	
	// Pseudo-filters

	public static bool NoRootTaskParent(Task task) {
		return true;
	}

	public static bool NoHierarchy(Task task) {
		return true;
	}

	public static bool WithPath(Task task) {
		return true;
	}
	
	public static bool WithPathWithoutFirst(Task task) {
		return true;
	}
}