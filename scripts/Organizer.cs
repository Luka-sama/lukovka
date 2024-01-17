using System.Collections.Generic;
using System.Linq;

public class Organizer {
	public static void Organize(List<Task> tasks) {
		var organizedTasks = tasks.Where(task => true/*Filter.apply(task)*/).ToList();
		organizedTasks.Sort();
	}
}