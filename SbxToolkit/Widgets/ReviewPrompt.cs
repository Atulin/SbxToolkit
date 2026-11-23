using Spectre.Console;

namespace SbxToolkit.Widgets;

public sealed record ReviewDecision<TKey, TValue>(string Label, TKey ItemId, TValue OptionId);

public static class ReviewPrompt
{
	public static ReviewDecision<TKey, TOptionKey>[] Run<T, TKey, TValue, TOptionKey>(
		IEnumerable<T> source,
		Func<T, string> display,
		Func<T, IEnumerable<TValue>> options,
		Func<TValue, string> optionLabel,
		Func<T, TKey> itemKey,
		Func<TValue, TOptionKey> optionKey,
		TValue defaultValue,
		int pageSize = 10)
		where TKey : notnull
		where TValue : notnull
	{
		var items = source
			.Select(item => new Entry<T, TValue>(
				item,
				[defaultValue, .. options(item)]))
			.ToArray();

		if (items.Length == 0)
		{
			return [];
		}

		if (items.Any(x => x.Options.Length == 0))
		{
			throw new ArgumentException("Every item must have at least one option.", nameof(source));
		}

		pageSize = Math.Max(1, pageSize);

		var selected = 0;
		var firstVisible = 0;
		var selections = new int[items.Length];
		Array.Fill(selections, 0);

		var result = AnsiConsole.Live(BuildView(items, display, optionLabel, selections, selected, firstVisible, pageSize))
			.Start(ctx => {
				while (true)
				{
					var key = Console.ReadKey(intercept: true).Key;
					var entry = items[selected];

					switch (key)
					{
						case ConsoleKey.UpArrow:
							selected = Math.Max(0, selected - 1);
							break;

						case ConsoleKey.DownArrow:
							selected = Math.Min(items.Length - 1, selected + 1);
							break;

						case ConsoleKey.PageUp:
							selected = Math.Max(0, selected - pageSize);
							break;

						case ConsoleKey.PageDown:
							selected = Math.Min(items.Length - 1, selected + pageSize);
							break;

						case ConsoleKey.Home:
							selected = 0;
							break;

						case ConsoleKey.End:
							selected = items.Length - 1;
							break;

						case ConsoleKey.LeftArrow:
							if (selections[selected] is var left)
							{
								selections[selected] = Math.Max(0, left - 1);
							}
							else
							{
								selections[selected] = entry.Options.Length - 1;
							}
							break;

						case ConsoleKey.RightArrow:
							selections[selected] = selections[selected] is {} right
								? (right + 1) % entry.Options.Length
								: 0;
							break;

						case ConsoleKey.Enter:
							return items
								.Select((item, index) => (item, index))
								.Select(x => new ReviewDecision<TKey, TOptionKey>(
									display(x.item.Value),
									itemKey(x.item.Value),
									optionKey(x.item.Options[selections[x.index]])))
								.ToArray();
					}

					if (selected < firstVisible)
					{
						firstVisible = selected;
					}

					if (selected >= firstVisible + pageSize)
					{
						firstVisible = selected - pageSize + 1;
					}

					ctx.UpdateTarget(BuildView(items, display, optionLabel, selections, selected, firstVisible, pageSize));
				}
			});

		return result;
	}

	private sealed record Entry<T, TValue>(T Value, TValue[] Options);

	private static Table BuildView<T, TValue>(
		Entry<T, TValue>[] items,
		Func<T, string> display,
		Func<TValue, string> optionLabel,
		int[] selections,
		int selected,
		int firstVisible,
		int pageSize)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Request")
			.AddColumn("Decision");

		var lastVisible = Math.Min(items.Length, firstVisible + pageSize);

		for (var i = firstVisible; i < lastVisible; i++)
		{
			var isSelected = i == selected;
			var name = Markup.Escape(display(items[i].Value));
			
			var selection = selections[i];

			var options = items[i].Options
				.Select((option, index) => {
					var label = Markup.Escape(optionLabel(option));

					if (selection == index)
					{
						return $"[black on cyan] {label} [/]";
					}

					return isSelected
						? $"[grey] {label} [/]"
						: $"[dim] {label} [/]";
				});

			var nameMarkup = isSelected
				? $"[black on cyan]> {name}[/]"
				: $"  {name}";

			table.AddRow(
				new Markup(nameMarkup),
				new Markup(string.Join(" ", options))
			);
		}

		return table;
	}
}