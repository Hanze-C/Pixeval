// Copyright (c) Pixeval.Controls.
// Licensed under the GPL v3 License.

using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Pixeval.Collections;

[DebuggerDisplay("Count = {Count}")]
public class AdvancedObservableCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IList<T>, IList, IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged, ISupportIncrementalLoading, IComparer<T> where T : class
{
    private readonly Dictionary<string, PropertyInfo> _sortProperties = [];

    private readonly bool _liveShapingEnabled;

    private readonly HashSet<string> _observedFilterProperties = [];

    private readonly List<T> _view = [];

    private IList ListView => RangedView;

    private WeakEventListener<AdvancedObservableCollection<T>, object?, NotifyCollectionChangedEventArgs> _sourceWeakEventListener = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedObservableCollection{T}"/> class.
    /// </summary>
    public AdvancedObservableCollection() : this([])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedObservableCollection{T}"/> class.
    /// </summary>
    /// <param name="source">source IEnumerable</param>
    /// <param name="isLiveShaping">Denotes whether this AOC should re-filter/re-sort if a PropertyChanged is raised for an observed property.</param>
    public AdvancedObservableCollection(ObservableCollection<T> source, bool isLiveShaping = false)
    {
        _liveShapingEnabled = isLiveShaping;
        SortDescriptions.CollectionChanged += SortDescriptionsCollectionChanged;
        Source = source;
        return;

        void SortDescriptionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleSortChanged();
    }

    /// <summary>
    /// Gets or sets the source
    /// </summary>
    public ObservableCollection<T> Source
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value, "Null is not allowed");

            if (field == value)
                return;

            DetachPropertyChangedHandler(field);
            field = value;
            AttachPropertyChangedHandler(field);

            _sourceWeakEventListener?.Detach();

            _sourceWeakEventListener = new(this)
            {
                OnEventAction = (source, changed, arg) => SourceNcc_CollectionChanged(source, arg),
                OnDetachAction = listener => field.CollectionChanged -= listener.OnEvent
            };
            field.CollectionChanged += _sourceWeakEventListener.OnEvent;

            HandleSourceChanged();
            OnPropertyChanged();
        }
    }

    #region IList<T>

    private List<T> RangedView
    {
        get
        {
            if (Equals(Range, Range.All))
                return _view;
            var viewCount = _view.Count;
            var start = Range.Start.GetOffset(viewCount);
            if (start > viewCount)
                return [];
            var end = Range.End.GetOffset(viewCount);
            if (end < 0)
                return [];
            if (start > end)
                return [];
            start = Math.Max(0, start);
            end = Math.Min(viewCount, end);
            return _view[start..end];
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => RangedView.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => RangedView.GetEnumerator();

    /// <inheritdoc />
    public void Add(T item) => Source.Add(item);

    /// <inheritdoc cref="ICollection{T}.Clear"/> />
    public void Clear() => Source.Clear();

    /// <inheritdoc />
    public bool Contains(T item) => RangedView.Contains(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex) => RangedView.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(T item) => Source.Remove(item);

    /// <inheritdoc cref="ICollection{T}.Count"/> />
    public int Count => RangedView.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public int IndexOf(T item) => RangedView.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, T item) => Source.Insert(index, item);

    /// <inheritdoc cref="IList{T}.RemoveAt"/> />
    public void RemoveAt(int index) => Source.Remove(RangedView[index]);

    /// <inheritdoc cref="List{T}.this[int]"/>
    public T this[int index]
    {
        get => RangedView[index];
        set => RangedView[index] = value;
    }

    #endregion

    /// <inheritdoc cref="ISupportIncrementalLoading.LoadMoreItemsAsync"/>
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => (Source as ISupportIncrementalLoading)?.LoadMoreItemsAsync(count) ?? Task.FromResult(new LoadMoreItemsResult()).AsAsyncOperation();

    /// <inheritdoc cref="ISupportIncrementalLoading.HasMoreItems"/>
    public bool HasMoreItems => (Source as ISupportIncrementalLoading)?.HasMoreItems ?? false;

    /// <summary>
    /// Gets or sets the predicate used to filter the visible items
    /// </summary>
    public Func<T, bool>? Filter
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            RaiseFilterChanged();
        }
    }

    public Range Range
    {
        get;
        set
        {
            if (field.Equals(value))
                return;

            field = value;
            OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
        }
    } = Range.All;

    /// <summary>
    /// Gets SortDescriptions to sort the visible items
    /// </summary>
    public ObservableCollection<ISortDescription<T>> SortDescriptions { get; } = [];

    /// <inheritdoc cref="IComparer{T}.Compare"/>
    int IComparer<T>.Compare(T? x, T? y)
    {
        if (_sortProperties.Count is 0)
            foreach (var sd in SortDescriptions)
                if (!string.IsNullOrEmpty(sd.PropertyName))
                    _sortProperties[sd.PropertyName] = typeof(T).GetProperty(sd.PropertyName)!;

        foreach (var sd in SortDescriptions)
        {
            int cmp;

            if (sd.PropertyMode)
            {
                var pi = _sortProperties[sd.PropertyName];

                cmp = Comparer.Default.Compare(pi.GetValue(x), pi.GetValue(y));
            }
            else
            {
                cmp = sd.Comparer.Compare(x, y);
            }

            if (cmp is not 0)
                return sd.Direction is SortDirection.Ascending ? +cmp : -cmp;
        }

        return 0;
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when the <see cref="Filter"/> changes.
    /// </summary>
    public event Action<AdvancedObservableCollection<T>, Func<T, bool>?>? FilterChanged;

    /// <summary>
    /// Property changed event invoker
    /// </summary>
    /// <param name="propertyName">name of the property that changed</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!) => PropertyChanged?.Invoke(this, new(propertyName));

    /// <summary>
    /// Raise CollectionChanged event to any listeners.
    /// Properties/methods modifying this ObservableCollection will raise
    /// a collection changed event through this virtual method.
    /// </summary>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

    /// <summary>
    /// Add a property to re-filter an item on when it is changed
    /// </summary>
    public void ObserveFilterProperty(string propertyName)
    {
        _ = _observedFilterProperties.Add(propertyName);
    }

    /// <summary>
    /// Remove a property to re-filter an item on when it is changed
    /// </summary>
    public void UnobserveFilterProperty(string propertyName)
    {
        _ = _observedFilterProperties.Remove(propertyName);
    }

    /// <summary>
    /// Clears all properties items are re-filtered on
    /// </summary>
    public void ClearObservedFilterProperties()
    {
        _observedFilterProperties.Clear();
    }

    private void ItemOnPropertyChanged(object? i, PropertyChangedEventArgs e)
    {
        if (!_liveShapingEnabled || i is not T item)
            return;

        var filterResult = Filter?.Invoke(item);

        if (filterResult.HasValue && _observedFilterProperties.Contains(e.PropertyName!))
        {
            var viewIndex = _view.IndexOf(item);
            if (viewIndex is not -1 && !filterResult.Value)
                RemoveFromView(viewIndex, item);
            else if (viewIndex is -1 && filterResult.Value)
            {
                var index = Source.IndexOf(item);
                _ = HandleItemAdded(index, item);
            }
        }

        if ((filterResult ?? true) && SortDescriptions.Any(sd => sd.PropertyName == e.PropertyName))
        {
            var oldIndex = _view.IndexOf(item);

            // Check if item is in view:
            if (oldIndex < 0)
                return;

            _view.RemoveAt(oldIndex);
            var targetIndex = _view.BinarySearch(item, this);
            if (targetIndex < 0)
                targetIndex = ~targetIndex;

            _view.Insert(targetIndex, item);

            // Only trigger expensive UI updates if the index really changed:
            if (targetIndex != oldIndex)
                OnCollectionChanged(new(NotifyCollectionChangedAction.Move, item, targetIndex, oldIndex));
        }
        else if (string.IsNullOrEmpty(e.PropertyName))
            HandleSourceChanged();
    }

    private void AttachPropertyChangedHandler(IEnumerable? items)
    {
        if (!_liveShapingEnabled || items is null)
            return;

        foreach (var item in items.OfType<INotifyPropertyChanged>())
            item.PropertyChanged += ItemOnPropertyChanged;
    }

    private void DetachPropertyChangedHandler(IEnumerable? items)
    {
        if (!_liveShapingEnabled || items == null)
            return;

        foreach (var item in items.OfType<INotifyPropertyChanged>())
            item.PropertyChanged -= ItemOnPropertyChanged;
    }

    private void HandleSortChanged()
    {
        _sortProperties.Clear();
        if (SortDescriptions.Count is not 0)
        {
            _view.Sort(this);
            _sortProperties.Clear();
        }
        else
        {
            var newIndex = 0;
            foreach (var item in Source)
            {
                if (_view.IndexOf(item) is var index and not -1)
                    // 元素重复时可能出现index < newIndex
                    if (index == newIndex)
                        ++newIndex;
                    else if (index > newIndex)
                    {
                        _view.RemoveAt(index);
                        _view.Insert(newIndex, item);
                        ++newIndex;
                    }
            }
        }
        OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    public void RaiseFilterChanged()
    {
        if (Filter is not null)
        {
            for (var index = 0; index < _view.Count; ++index)
            {
                var item = _view[index];
                if (Filter(item))
                    continue;

                RemoveFromView(index, item);
                index--;
            }
        }

        var viewHash = this.ToFrozenSet();
        var viewIndex = 0;
        for (var index = 0; index < Source.Count; ++index)
        {
            var item = Source[index];
            if (viewHash.Contains(item))
            {
                ++viewIndex;
                continue;
            }

            if (HandleItemAdded(index, item, viewIndex))
                ++viewIndex;
        }

        FilterChanged?.Invoke(this, Filter);
    }

    private void HandleSourceChanged()
    {
        _sortProperties.Clear();
        _view.Clear();
        foreach (var item in Source)
        {
            if (Filter is not null && !Filter(item))
                continue;

            if (SortDescriptions.Count is not 0)
            {
                var targetIndex = _view.BinarySearch(item, this);
                if (targetIndex < 0)
                    targetIndex = ~targetIndex;

                _view.Insert(targetIndex, item);
            }
            else
            {
                _view.Add(item);
            }
        }

        _sortProperties.Clear();
        OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }

    private void SourceNcc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                AttachPropertyChangedHandler(e.NewItems);
                if (e.NewItems is [T item])
                    _ = HandleItemAdded(e.NewStartingIndex, item);
                else
                    HandleSourceChanged();

                break;
            }
            case NotifyCollectionChangedAction.Remove:
            {
                DetachPropertyChangedHandler(e.OldItems);
                if (e.OldItems is [T item])
                    HandleItemRemoved(e.OldStartingIndex, item);
                else
                    HandleSourceChanged();

                break;
            }
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Reset:
            {
                HandleSourceChanged();
                break;
            }
        }
    }

    private bool HandleItemAdded(int sourceIndex, T newItem, int? viewIndex = null)
    {
        if (Filter is not null && !Filter(newItem))
            return false;

        var newViewIndex = _view.Count;

        if (SortDescriptions.Count is not 0)
        {
            _sortProperties.Clear();
            newViewIndex = _view.BinarySearch(newItem, this);
            if (newViewIndex < 0)
                newViewIndex = ~newViewIndex;
        }
        else if (sourceIndex is 0 || _view.Count is 0)
            newViewIndex = 0;
        else if (viewIndex.HasValue)
            newViewIndex = viewIndex.Value;
        else if (_view.Count == Source.Count - 1)
            newViewIndex = _view.Count;
        else
        {
            for (int i = 0, j = 0; i < Source.Count; ++i)
            {
                if (i == sourceIndex || j >= _view.Count)
                {
                    newViewIndex = j;
                    break;
                }

                if (_view[j] == Source[i])
                    j++;
            }
        }

        _view.Insert(newViewIndex, newItem);

        var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, newViewIndex);
        OnCollectionChanged(e);
        return true;
    }

    private void HandleItemRemoved(int oldStartingIndex, T oldItem)
    {
        if (Filter is not null && !Filter(oldItem))
            return;

        if (oldStartingIndex < 0 || oldStartingIndex >= _view.Count || !Equals(_view[oldStartingIndex], oldItem))
            oldStartingIndex = _view.IndexOf(oldItem);

        if (oldStartingIndex < 0)
            return;
        RemoveFromView(oldStartingIndex, oldItem);
    }

    private void RemoveFromView(int itemIndex, T item)
    {
        _view.RemoveAt(itemIndex);
        var e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, itemIndex);
        OnCollectionChanged(e);
    }

    #region IList

    int IList.Add(object? value) => ListView.Add(value);

    bool IList.Contains(object? value) => ListView.Contains(value);

    int IList.IndexOf(object? value) => ListView.IndexOf(value);

    void IList.Insert(int index, object? value) => ListView.Insert(index, value);

    void IList.Remove(object? value) => ListView.Remove(value);

    void ICollection.CopyTo(Array array, int index) => ListView.CopyTo(array, index);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => ListView.SyncRoot;

    bool IList.IsReadOnly => false;

    object? IList.this[int index]
    {
        get => ListView[index];
        set => ListView[index] = value;
    }

    bool IList.IsFixedSize => false;

    #endregion
}

/// <summary>
/// Sort description
/// </summary>
public class SortDescription<T> : ISortDescription<T>
{
    /// <inheritdoc />
    public bool PropertyMode { get; }

    /// <inheritdoc />
    public string? PropertyName { get; }

    /// <inheritdoc />
    public SortDirection Direction { get; }

    /// <inheritdoc />
    public IComparer<T>? Comparer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortDescription"/> class that describes
    /// a sort on the object itself
    /// </summary>
    /// <param name="comparer">Comparer to use. If null, will use default comparer</param>
    /// <param name="direction">Direction of sort</param>
    public SortDescription(IComparer<T>? comparer, SortDirection direction)
    {
        Direction = direction;
        Comparer = comparer;
        PropertyMode = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortDescription"/> class.
    /// </summary>
    /// <param name="propertyName">Name of property to sort on</param>
    /// <param name="direction">Direction of sort</param>
    public SortDescription(string propertyName, SortDirection direction)
    {
        PropertyName = propertyName;
        Direction = direction;
        PropertyMode = true;
    }
}

public interface ISortDescription<in T>
{
    [MemberNotNullWhen(true, nameof(PropertyName))]
    [MemberNotNullWhen(false, nameof(Comparer))]
    bool PropertyMode { get; }

    /// <summary>
    /// Gets the name of property to sort on
    /// </summary>
    string? PropertyName { get; }

    /// <summary>
    /// Gets the direction of sort
    /// </summary>
    SortDirection Direction { get; }

    /// <summary>
    /// Gets the comparer
    /// </summary>
    IComparer<T>? Comparer { get; }
}
