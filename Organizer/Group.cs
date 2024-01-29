using System;
using System.Reflection;

public class Group {
	public static readonly MethodInfo[] AllGroupings = typeof(Group)
		.GetMethods(BindingFlags.Static | BindingFlags.Public);
	
	public static string ByDate(Task task) {
		return DateToString(task.Date);
	}

	public static string ByCompleted(Task task) {
		return DateToString(task.Completed);
	}

	private static string DateToString(DateTime dateTime) {
		return DateOnly.FromDateTime(dateTime.ToLocalTime()).ToString();
	}
}