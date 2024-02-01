using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

public partial class Filter : GodotObject {
	private static readonly FieldInfo[] Fields = typeof(Task)
		.GetFields(BindingFlags.Public | BindingFlags.Instance)
		.Where(field => !Attribute.IsDefined(field, typeof(JsonIgnoreAttribute)))
		.ToArray();
	private static readonly string[] FieldNames = Fields
		.Select(field => char.ToLower(field.Name[0]) + (field.Name.Length == 1 ? "" : field.Name[1..]))
		.Append("now")
		.ToArray();
	private static readonly DateTime Epoch = new(1970, 1, 1);
	private Task _task;

	public void SetTask(Task task) {
		_task = task;
	}
	
	public bool NotCompleted() {
		return _task.Completed == DateTime.MinValue;
	}
	
	public bool Completed() {
		return _task.Completed != DateTime.MinValue;
	}
	
	public bool CompletedLastMonth() {
		return _task.Completed.AddMonths(1) >= DateTime.Now;
	}
	
	public bool CompletedLastYear() {
		return _task.Completed.AddYears(1) >= DateTime.Now;
	}

	public bool WithDate() {
		return _task.Date != DateTime.MinValue;
	}

	public bool NextMonth() {
		return _task.Date >= DateTime.Now && _task.Date <= DateTime.Now.AddMonths(1);
	}

	public bool NextYear() {
		return _task.Date >= DateTime.Now && _task.Date <= DateTime.Now.AddYears(1);
	}

	public bool NoTasksWithChildren() {
		return _task.Children.Count < 1;
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
		var values = Fields
			.Select(field => {
				return (Variant)(field.GetValue(_task) switch {
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
		var result = expression.Execute(new Array(values), this);
		if (!expression.HasExecuteFailed()) {
			return (bool)result;
		}
		return false;
	}
}