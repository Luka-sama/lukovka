using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

public static class Customizer {
	public static readonly DateTime Epoch = new(1970, 1, 1);
	private static readonly FieldInfo[] Fields = typeof(Task)
		.GetFields(BindingFlags.Public | BindingFlags.Instance)
		.Where(field => !Attribute.IsDefined(field, typeof(JsonIgnoreAttribute)))
		.ToArray();
	private static readonly string[] FieldNames = Fields
		.Select(field => char.ToLower(field.Name[0]) + (field.Name.Length == 1 ? "" : field.Name[1..]))
		.Append("now")
		.ToArray();

	public static T CalcExpression<[MustBeVariant] T>(
		Task task, string expressionString, GodotObject baseInstance, T defaultValue
	) {
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
		var result = expression.Execute(new Array(values), baseInstance);
		return (!expression.HasExecuteFailed() ? result.As<T>() : defaultValue);
	}
}