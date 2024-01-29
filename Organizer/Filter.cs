using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Array = Godot.Collections.Array;

public partial class Filter : GodotObject {
	public static readonly MethodInfo[] AllFilters = typeof(Filter)
		.GetMethods(BindingFlags.Static | BindingFlags.Public);
	
	private static readonly FieldInfo[] Fields = typeof(Task)
		.GetFields(BindingFlags.Public | BindingFlags.Instance)
		.Where(field => !Attribute.IsDefined(field, typeof(JsonIgnoreAttribute)))
		.ToArray();
	private static readonly SnakeCaseNamingStrategy SnakeCaseStrategy = new();
	
	private static readonly string[] FieldNames = Fields
		.Select(field => SnakeCaseStrategy.GetPropertyName(field.Name, false))
		.Concat(AllFilters.Select(filter => SnakeCaseStrategy.GetPropertyName(filter.Name, false)))
		.Append("now")
		.ToArray();
	private static readonly DateTime Epoch = new(1970, 1, 1);
	
	public static bool NotCompleted(Task task) {
		return task.Completed == DateTime.MinValue;
	}
	
	public static bool Completed(Task task) {
		return task.Completed != DateTime.MinValue;
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

	public static bool NextMonth(Task task) {
		return task.Date >= DateTime.Now && task.Date <= DateTime.Now.AddMonths(1);
	}

	public static bool NextYear(Task task) {
		return task.Date >= DateTime.Now && task.Date <= DateTime.Now.AddYears(1);
	}

	public static bool NoTasksWithChildren(Task task) {
		return task.Children.Count < 1;
	}

	public static bool Prioritized(Task task) {
		return task.Priority > 0;
	}
	
	// Pseudo-filters

	public static bool NoRootTaskParent(Task _) {
		return true;
	}

	public static bool NoHierarchy(Task _) {
		return true;
	}

	public static bool WithPath(Task _) {
		return true;
	}
	
	public static bool WithPathWithoutFirst(Task _) {
		return true;
	}

	public static bool NoCompleteButton(Task _) {
		return true;
	}

	public static bool Custom(Task task, string expressionString) {
		var values = Fields
			.Select(field => {
				return (Variant)(field.GetValue(task) switch {
					bool flag => flag,
					int num => num,
					double num => num,
					string str => str,
					DateTime dateTime => (
						dateTime != DateTime.MinValue ? (dateTime.ToUniversalTime() - Epoch).TotalSeconds : 0
					),
					List<string> list => new Array(list.Select(value => Variant.From(value))),
					null when field.FieldType == typeof(List<string>) => new Array(),
					_ => throw new ArgumentException()
				});
			})
			.Append((DateTime.Now.ToUniversalTime() - Epoch).TotalSeconds);
		var expression = new Expression();
		expression.Parse(expressionString, FieldNames);
		var result = expression.Execute(new Array(values));
		if (!expression.HasExecuteFailed()) {
			return (bool)result;
		}

		Variant a = new Callable(() => true);
		return false;
	}
}