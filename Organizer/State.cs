using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class State {
	public static State Default => new() {
		Name = "⌂",
		SelectedFilters = { "NotCompleted" },
	};
	public string Name;
	public List<string> SelectedFilters = new();
	public string GroupBy = "";
	public string SelectedSort = "Standard";
	public int RootId;
	public bool DescendingSort;
	public bool Expanded;

	public State Clone(string newName) {
		var state = Deserialize(Serialize());
		state.Name = newName;
		return state;
	}

	public bool Equals(State state) {
		string savedNameA = Name, savedNameB = state.Name;
		List<string> filtersA = SelectedFilters.ToList(), filtersB = state.SelectedFilters.ToList();
		Name = "";
		state.Name = "";
		SelectedFilters.Sort();
		state.SelectedFilters.Sort();
		var result = (Serialize() == state.Serialize());
		Name = savedNameA;
		state.Name = savedNameB;
		SelectedFilters = filtersA;
		state.SelectedFilters = filtersB;
		return result;
	}

	public void Create() {
		var json = Serialize();
		App.Request(HttpClient.Method.Put, json, Endpoint.States);
	}

	public void Delete() {
		App.Request(HttpClient.Method.Delete, Name, Endpoint.States);
	}

	private static State Deserialize(string json) {
		return JsonConvert.DeserializeObject<State>(json);
	}

	private string Serialize() {
		return JsonConvert.SerializeObject(this);
	}
}
