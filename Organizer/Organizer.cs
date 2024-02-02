using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;

public partial class Organizer : Control {
	public static List<Task> Tasks { get; private set; }
	public static State State { get; private set; } = State.Default;
	private static readonly Sort Sort = new();
	private static readonly Filter Filter = new();
	private static readonly Group Group = new();
	private static readonly Dictionary<string, State> States = new();
	private static readonly MethodInfo[] AllSorts = typeof(Sort)
		.GetMethods(BindingFlags.Instance | BindingFlags.Public)
		.Where(method => method.DeclaringType == typeof(Sort))
		.ToArray();
	private static readonly MethodInfo[] AllFilters = typeof(Filter)
		.GetMethods(BindingFlags.Instance | BindingFlags.Public)
		.Where(method => method.DeclaringType == typeof(Filter))
		.ToArray();
	private static readonly MethodInfo[] AllGroupings = typeof(Group)
		.GetMethods(BindingFlags.Instance | BindingFlags.Public)
		.Where(method => method.DeclaringType == typeof(Group))
		.ToArray();
	private static FontFile _boldFont;
	private static Control _states;
	private static Control _stateManager;
	private static PopupMenu _filterMenu;
	private static OptionButton _sortMenu;
	private static OptionButton _groupingMenu;
	private static Button _expandButton;
	
	public static void Organize() {
		UpdateStateButtons();
		Tasks = App.Tasks.Values.ToList();
		FilterTasks();
		SortTasks();
		GroupTasks();
	}

	public static void Clear() {
		Sort.Free();
		Filter.Free();
		Group.Free();
	}

	public override void _Ready() {
		_boldFont = GD.Load<FontFile>("res://fonts/verdanab.ttf");
		_states = GetNode<Control>("%States");
		_stateManager = GetNode<Control>("%StateManager");
		_expandButton = GetNode<Button>("%Expand");

		_sortMenu = GetNode<OptionButton>("%Sort");
		foreach (var sort in AllSorts) {
			_sortMenu.AddItem(TransformName(sort.Name));
		}

		_groupingMenu = GetNode<OptionButton>("%GroupBy");
		foreach (var grouping in AllGroupings) {
			_groupingMenu.AddItem(TransformName(grouping.Name));
		}

		_filterMenu = GetNode<MenuButton>("%AddFilter").GetPopup();
		_filterMenu.IdPressed += AddFilterByIndex;
		
		States.Add(State.Name, State.Default);
		AddStateButton(State.Name);
		ApplyState(State, true);
		UpdateStateButtons();
		State.Name = "";
	}

	public static bool HasFilter(string filterName) {
		return State.SelectedFilters.Contains(filterName);
	}
	
	public static void RemoveFilter(string filterName) {
		if (!HasFilter(filterName)) {
			return;
		}
		
		if (filterName == "NoHierarchy") {
			RemoveFilter("NoTasksWithChildren");
			State.GroupBy = "";
			_groupingMenu.Select(0);
		}
		
		State.SelectedFilters.Remove(filterName);
		_stateManager.GetNode(filterName.ValidateNodeName()).QueueFree();
		RegenerateFilterList();
		App.View.Render();
	}

	public static void AddOrRestoreState(State state) {
		States.Add(state.Name, state);
		AddStateButton(state.Name);
		UpdateStateButtons();
	}

	private static void FilterTasks() {
		Tasks = Tasks
			.Where(HasFilter("NoRootTaskParent") ? IsChildOfRoot : WithHierarchy(IsChildOfRoot))
			.Where(HasFilter("NoHierarchy") ? SumFilter : WithHierarchy(SumFilter))
			.ToList();
	}

	private static void SortTasks() {
		MethodInfo sort = null;
		if (!State.SelectedSort.StartsWith("@")) {
			sort = AllSorts.First(sortInfo => sortInfo.Name == State.SelectedSort);
		}
		Tasks.Sort((a, b) => {
			Sort.A = a;
			Sort.B = b;
			int cmp;
			if (sort == null) {
				cmp = Sort.Custom(State.SelectedSort[1..]);
			} else {
				cmp = (int)sort.Invoke(Sort, Array.Empty<object>())!;
			}
			return (State.DescendingSort ? -cmp : cmp);
		});
	}

	private static void GroupTasks() {
		if (string.IsNullOrEmpty(State.GroupBy)) {
			return;
		}
		var groups = new Dictionary<string, Task>();
		var groupTasks = new List<Task>();
		var nextId = -1;
		MethodInfo grouping = null;
		if (!State.GroupBy.StartsWith("@")) {
			grouping = AllGroupings.First(groupingInfo => groupingInfo.Name == State.GroupBy);
		}
		foreach (var task in Tasks) {
			Group.Task = task;
			string group;
			if (grouping == null) {
				group = Group.Custom(State.GroupBy[1..]);
			} else {
				group = (string)grouping.Invoke(Group, Array.Empty<object>())!;
			}
			if (groups.TryGetValue(group, out var groupTask)) {
				task.Group = groupTask.Id;
				continue;
			}

			groupTask = new Task() {
				Expanded = true,
				Id = nextId,
				Text = group,
				Folder = true,
			};
			groupTasks.Add(groupTask);
			task.Group = groupTask.Id;
			groups.Add(group, groupTask);
			nextId--;
		}
		Tasks.AddRange(groupTasks);
	}

	private static Func<Task, bool> WithHierarchy(Func<Task, bool> filter) {
		return task => filter(task) || task.Children.Any(WithHierarchy(filter));
	}

	private static bool SumFilter(Task task) {
		if (!IsInstanceValid(Filter)) {
			return false;
		}
		Filter.Task = task;
		var result = AllFilters
			.Where(filterInfo => HasFilter(filterInfo.Name))
			.All(filterInfo => (bool)filterInfo.Invoke(Filter, Array.Empty<object>())!);
		if (!result) {
			return false;
		}
		return State.SelectedFilters
			.Where(filterName => filterName.StartsWith("@"))
			.All(customFilter => Filter.Custom(customFilter[1..]));
	}
	
	private static void RegenerateFilterList() {
		_filterMenu.Clear(true);
		foreach (var filter in AllFilters) {
			if (!HasFilter(filter.Name)) {
				_filterMenu.AddItem(TransformName(filter.Name));
			}
		}
	}

	private static bool IsChildOfRoot(Task task) {
		if (State.RootId == 0) {
			return true;
		}
		
		if (task.Id == State.RootId) {
			return true;
		} else if (!App.Tasks.ContainsKey(task.Parent)) {
			if (task.Parent != 0) {
				ErrorDialog.Show($"Orphan task with ID {task.Id}.", true);
			}
			return false;
		}
		return IsChildOfRoot(App.Tasks[task.Parent]);
	}
	
	private static string TransformName(string input) {
		if (input.StartsWith("@")) {
			return input;
		}
		
		var result = new StringBuilder();
		result.Append(input[0]);
		for (var i = 1; i < input.Length; i++) {
			if (char.IsUpper(input[i])) {
				result.Append(' ');
			}
			result.Append(char.ToLower(input[i]));
		}
		return result.ToString();
	}

	private static void ExpandTasks() {
		if (HasFilter("NoHierarchy")) {
			ErrorDialog.Show("You can't expand tasks with no hierarchy.");
			return;
		}
		
		State.Expanded = !State.Expanded;
		ApplyExpand();
		App.View.Render();
	}
	
	private static void ApplyExpand() {
		_expandButton.Text = (State.Expanded ? "Collapse" : "Expand");
		foreach (var task in App.Tasks.Values) {
			task.Expanded = State.Expanded && task.Children.Count > 0;
		}
	}

	private static void ChangeSort(long index) {
		var sortName = AllSorts[(int)index - 1].Name;
		if (sortName == "Custom") {
			var startValue = (State.SelectedSort.StartsWith("@") ? State.SelectedSort[1..] : "");
			PromptDialog.Show("Enter the custom sort", AddCustomSort, startValue);
			return;
		}
		
		State.DescendingSort = (State.SelectedSort == sortName && !State.DescendingSort);
		State.SelectedSort = sortName;
		App.View.Render();
	}

	private static void ChangeGrouping(long index) {
		var groupBy = AllGroupings[(int)index - 1].Name;
		if (groupBy == "Custom") {
			var startValue = (State.GroupBy.StartsWith("@") ? State.GroupBy[1..] : "");
			PromptDialog.Show("Enter the custom grouping", AddCustomGroup, startValue);
			return;
		}
		
		AddFilter("NoHierarchy");
		State.GroupBy = (State.GroupBy != groupBy ? groupBy : "");
		if (string.IsNullOrEmpty(State.GroupBy)) {
			_groupingMenu.Select(0);
		}
		App.View.Render();
	}
	
	private static void AddFilterByIndex(long index) {
		var filterName = AllFilters
			.Where(filter => !HasFilter(filter.Name))
			.ToList()[(int)index].Name;
		AddFilter(filterName);
	}

	private static void AddFilter(string filterName) {
		if (HasFilter(filterName)) {
			return;
		}
		
		switch (filterName) {
			case "Custom":
				var validated = filterName.ValidateNodeName();
				if (State.SelectedFilters.Exists(filter => filter.ValidateNodeName() == validated)) {
					ErrorDialog.Show($"There is already a custom filter with content {filterName}.");
					return;
				}
				PromptDialog.Show("Enter the custom filter", AddCustomFilter);
				return;
			case "NoTasksWithChildren" when !HasFilter("NoHierarchy"):
				ErrorDialog.Show("This filter can only be added with no hierarchy filter.");
				return;
			case "NoRootTaskParent" when State.RootId == 0 || App.Tasks[State.RootId].Parent == 0:
				ErrorDialog.Show("This filter can only be added with a root task which has a parent.");
				return;
			case "NoHierarchy":
				State.Expanded = false;
				ApplyExpand();
				break;
			case "NotCompleted":
				RemoveFilter("Completed");
				break;
			case "Completed":
			case "CompletedLastDay":
			case "CompletedLastWeek":
			case "CompletedLastMonth":
			case "CompletedLastYear":
				RemoveFilter("NotCompleted");
				RemoveFilter("Completed");
				RemoveFilter("CompletedLastDay");
				RemoveFilter("CompletedLastWeek");
				RemoveFilter("CompletedLastMonth");
				RemoveFilter("CompletedLastYear");
				break;
			case "WithPath":
				RemoveFilter("WithPathWithoutFirst");
				break;
			case "WithPathWithoutFirst":
				RemoveFilter("WithPath");
				break;
		}

		State.SelectedFilters.Add(filterName);
		AddFilterButton(filterName);
		App.View.Render();
		RegenerateFilterList();
	}

	private static void AddCustomSort(string customSort) {
		State.DescendingSort = false;
		State.SelectedSort = "@" + customSort;
		App.View.Render();
	}

	private static void AddCustomFilter(string customFilter) {
		State.SelectedFilters.Add("@" + customFilter);
		AddFilterButton("@" + customFilter);
		App.View.Render();
	}

	private static void AddCustomGroup(string customGroup) {
		AddFilter("NoHierarchy");
		State.GroupBy = "@" + customGroup;
		App.View.Render();
	}

	private static void AddFilterButton(string filterName) {
		var filterButton = new Button();
		filterButton.Name = filterName.ValidateNodeName();
		filterButton.Text = TransformName(filterName);
		filterButton.Pressed += () => RemoveFilter(filterName);
		_stateManager.AddChild(filterButton);
		_stateManager.MoveChild(filterButton, -2);
	}
	
	private static void UpdateStateButtons() {
		foreach (var child in _states.GetChildren().SkipLast(1)) {
			if (child is not Button stateButton) {
				continue;
			}
			var state = States[stateButton.Name];
			if (State.Equals(state)) {
				stateButton.AddThemeFontOverride("font", _boldFont);
			} else {
				stateButton.RemoveThemeFontOverride("font");
			}
		}
	}

	private static void AddState(string stateName) {
		if (States.ContainsKey(stateName)) {
			ErrorDialog.Show($"There is already a state with name {stateName}.");
			return;
		} else if (stateName.ValidateNodeName() != stateName) {
			ErrorDialog.Show($"The name {stateName} contains characters that are not allowed (. : @ / \" %).");
			return;
		}

		var state = State.Clone(stateName);
		AddOrRestoreState(state);
		state.Create();
	}

	private static void AddStateButton(string stateName) {
		var stateButton = new Button();
		stateButton.Name = stateName;
		stateButton.Text = stateName;
		stateButton.Pressed += () => ChangeState(stateName);
		_states.AddChild(stateButton);
		_states.MoveChild(stateButton, -2);
	}

	private static void AskStateName() {
		PromptDialog.Show("Enter the state name", AddState);
	}
	
	private static void RemoveState(string stateToRemove) {
		_states.GetNode(stateToRemove).QueueFree();
		States[stateToRemove].Delete();
		States.Remove(stateToRemove);
	}

	private static void ApplyState(State newState, bool first = false) {
		if (!first) {
			foreach (var filterName in State.SelectedFilters) {
				_stateManager.GetNode(filterName.ValidateNodeName()).Free();
			}
		}
		State = newState;
		foreach (var filterName in State.SelectedFilters) {
			AddFilterButton(filterName);
		}
		
		var sort = AllSorts.First(sort => sort.Name == State.SelectedSort);
		_sortMenu.Select(Array.IndexOf(AllSorts, sort) + 1);
		
		if (!string.IsNullOrEmpty(State.GroupBy)) {
			var grouping = AllGroupings.First(
				grouping => grouping.Name == (State.GroupBy.StartsWith("@") ? "Custom" : State.GroupBy)
			);
			_groupingMenu.Select(Array.IndexOf(AllGroupings, grouping) + 1);
		} else {
			_groupingMenu.Select(0);
		}
		
		ApplyExpand();
		RegenerateFilterList();
		List.FocusTask(State.RootId);
	}

	private static void ChangeState(string stateName) {
		var newState = States[stateName];
		if (stateName != State.Default.Name && State.Equals(newState)) {
			ConfirmDialog.Show($"Delete state {stateName}?", () => RemoveState(stateName));
			return;
		}
		
		ApplyState(newState.Clone(""));
		App.View.Render();
	}
}