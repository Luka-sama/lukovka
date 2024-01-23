using System.Collections.Generic;
using System.Linq;

public class Organizer {
	public static List<Task> Tasks { get; private set; }
	public static int RootId { get; set; }
	
	public static void Organize() {
		var tasks = App.Tasks.Values.ToList();
		tasks = tasks.Where(Filter.IsChildOfRoot).ToList();
		Tasks = tasks;
	}
}