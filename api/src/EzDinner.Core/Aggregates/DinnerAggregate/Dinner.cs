using EzDinner.Core.Aggregates.Shared;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EzDinner.Core.Aggregates.DinnerAggregate
{
  public class Dinner : AggregateRoot<Guid>
  {
    private readonly List<MenuItem> _menu;
    private readonly List<Tag> _tags;
    private readonly Dictionary<Guid, Guid> _dishChangeIds;

    public Guid ChangeId { get; private set; }
    public Guid OptOutChangeId { get; private set; }
    public IReadOnlyDictionary<Guid, Guid> DishChangeIds => new System.Collections.ObjectModel.ReadOnlyDictionary<Guid, Guid>(_dishChangeIds);

    public LocalDate Date { get; }
    public Guid FamilyId { get; private set; }
    public IEnumerable<MenuItem> Menu { get => _menu; }
    public IEnumerable<Tag> Tags { get => _tags; }
    public OptOut? OptOut { get; private set; }
    public bool IsPlanned => Menu.Any();
    public bool IsOptedOut => OptOut is not null;
    public bool IsResolved => IsPlanned || IsOptedOut;


    /// <summary>
    /// For serialization purpose only
    /// </summary>
    public Dinner(Guid id, Guid familyId, LocalDate date, IEnumerable<MenuItem> menu, IEnumerable<Tag> tags, OptOut? optOut = null,
      Guid changeId = default, Guid optOutChangeId = default, IReadOnlyDictionary<Guid, Guid>? dishChangeIds = null) : base(id)
    {
      Date = date;
      FamilyId = familyId;
      _menu = menu.ToList();
      _tags = tags.ToList();
      OptOut = optOut;
      ChangeId = changeId;
      OptOutChangeId = optOutChangeId;
      _dishChangeIds = dishChangeIds?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new();
    }

    /// <summary>
    /// Create a new Dinner for a given calendar date. The date is relative to the family and does not consider timezones or time in any way.
    /// </summary>
    /// <param name="familyId"></param>
    /// <param name="date"></param>
    /// <returns></returns>
    public static Dinner CreateNew(Guid familyId, LocalDate date)
    {
      return new Dinner(id: DinnerIdentityFactory.Create(familyId, date), familyId, date, menu: new List<MenuItem>(), tags: new List<Tag>());
    }

    /// <summary>
    /// Sets an opt-out reason for this dinner and clears any existing menu items.
    /// Mutually exclusive with having planned dishes.
    /// </summary>
    public void SetOptOut(OptOut optOut)
    {
      foreach (var item in _menu) RecordMenuChange(item.DishId);
      OptOut = optOut;
      _menu.Clear();
      RecordOptOutChange();
    }

    /// <summary>
    /// Sets an opt-out reason for this dinner and clears any existing menu items.
    /// Mutually exclusive with having planned dishes.
    /// </summary>
    public void SetOptOut(string reason) => SetOptOut(new OptOut(reason));

    /// <summary>
    /// Removes the opt-out, leaving the dinner unresolved.
    /// </summary>
    public void RemoveOptOut()
    {
      if (OptOut is null) return;
      OptOut = null;
      RecordOptOutChange();
    }

    /// <summary>
    /// Appends an item to the menu. Clears any opt-out (mutually exclusive).
    /// </summary>
    /// <param name="dishId"></param>
    public void AddMenuItem(MenuItem menuItem)
    {
      RemoveOptOut();
      var dishIsAlreadyAdded = _menu.Any(w => w == menuItem);
      if (dishIsAlreadyAdded) return;
      _menu.Add(menuItem);
      RecordMenuChange(menuItem.DishId);
    }

    /// <summary>
    /// Removes a dish from the menu. Does nothing if dish does not
    /// exist on the menu.
    /// </summary>
    /// <param name="dishId"></param>
    public void RemoveMenuItem(MenuItem menuItem)
    {
      var itemOnMenu = _menu.FirstOrDefault(w => w.Equals(menuItem));
      if (itemOnMenu is null) return;
      _menu.Remove(itemOnMenu);
      RecordMenuChange(menuItem.DishId);
    }

    public bool ReplaceMenuItem(MenuItem old, MenuItem replacement)
    {
      var menuItemIndex = _menu.FindIndex(w => w.Equals(old));
      if (menuItemIndex == -1) return false;
      _menu[menuItemIndex] = replacement;
      RecordMenuChange(old.DishId);
      RecordMenuChange(replacement.DishId);
      return true;
    }

    public bool UndoMenuChange(Guid dishId, DinnerStateValueObject before, DinnerStateValueObject after)
    {
      var wasPresent = before.DishIds.Contains(dishId);
      var becamePresent = after.DishIds.Contains(dishId);
      if (wasPresent == becamePresent ||
          !before.DishIds.Where(id => id != dishId).ToHashSet().SetEquals(after.DishIds.Where(id => id != dishId)) ||
          after.OptOutReason is not null || (wasPresent && before.OptOutReason is not null))
        throw new ArgumentException("INVALID_UNDO_ACTION");
      if (before.OptOutReason is not null)
      {
        if (after.ChangeId == Guid.Empty || after.ChangeId != ChangeId || !after.Matches(this)) return false;
        SetOptOut(before.OptOutReason);
        return true;
      }
      if (after.DishChangeId == Guid.Empty || after.DishChangeId != _dishChangeIds.GetValueOrDefault(dishId) ||
          after.OptOutChangeId != OptOutChangeId || IsOptedOut || Menu.Any(item => item.DishId == dishId) != becamePresent) return false;
      if (wasPresent) AddMenuItem(new MenuItem(dishId));
      if (!wasPresent) RemoveMenuItem(new MenuItem(dishId));
      return true;
    }

    private void RecordMenuChange(Guid dishId)
    {
      ChangeId = Guid.NewGuid();
      _dishChangeIds[dishId] = ChangeId;
    }

    private void RecordOptOutChange()
    {
      ChangeId = Guid.NewGuid();
      OptOutChangeId = ChangeId;
    }
  }
}
