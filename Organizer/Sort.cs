using System;
using System.Reflection;

public class Sort {
	public static readonly MethodInfo[] AllSorts = typeof(Sort)
		.GetMethods(BindingFlags.Static | BindingFlags.Public);
	
	public static int Standard(Task a, Task b) {
		if (a.Completed == DateTime.MinValue && b.Completed != DateTime.MinValue) {
			return -1;
		} else if (a.Completed != DateTime.MinValue && b.Completed == DateTime.MinValue) {
			return 1;
		}

		var orderA = (a.Order != 0 ? a.Order : a.Id);
		var orderB = (b.Order != 0 ? b.Order : b.Id);
		return (orderA > orderB ? 1 : -1);
	}

	public static int History(Task a, Task b) {
		if (a.Completed == DateTime.MinValue && b.Completed != DateTime.MinValue) {
			return 1;
		} else if (a.Completed != DateTime.MinValue && b.Completed == DateTime.MinValue) {
			return -1;
		}
		
		return Standard(a, b);
	}

	public static int ByDate(Task a, Task b) {
		return ByAnyDate(a, b, a.Date, b.Date);
	}

	public static int ByCompleted(Task a, Task b) {
		return ByAnyDate(a, b, a.Completed, b.Completed);
	}

	public static int ByPriority(Task a, Task b) {
		if (a.Priority == b.Priority) {
			return Standard(a, b);
		}
		return (a.Priority > b.Priority ? 1 : -1);
	}

	private static int ByAnyDate(Task a, Task b, DateTime dateA, DateTime dateB) {
		if (dateA != DateTime.MinValue && dateB == DateTime.MinValue) {
			return -1;
		} else if (dateA == DateTime.MinValue && dateB != DateTime.MinValue) {
			return 1;
		}

		if (dateA != dateB) {
			return (dateA < dateB ? 1 : -1);
		}

		return Standard(a, b);
	}
}