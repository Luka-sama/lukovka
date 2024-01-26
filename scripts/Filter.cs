using System;
using System.Linq;

public class Filter {
	public static Func<Task, bool> WithHierarchy(Func<Task, bool> filter) {
		return task => (
			filter(task) || App.Tasks.Values.Any(child => child.Parent == task.Id && WithHierarchy(filter)(child))
		);
	}

	public static bool IsChildOfRoot(Task task) {
		if (Organizer.RootId == 0) {
			return true;
		}
		
		if (task.Id == Organizer.RootId) {
			return true;
		} else if (!App.Tasks.ContainsKey(task.Parent)) {
			return false;
		}
		return IsChildOfRoot(App.Tasks[task.Parent]);
	}

	public static bool NotCompleted(Task task) {
		return task.Completed == DateTime.MinValue;
	}
}