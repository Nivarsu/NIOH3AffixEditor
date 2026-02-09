using Nioh3AffixEditor.Infrastructure;
using Nioh3AffixEditor.Models;
using Nioh3AffixEditor.Services;
using System.Linq;

namespace Nioh3AffixEditor.ViewModels;

public sealed class EquipmentViewModel : ObservableObject
{
    private readonly UnderworldSkillTable _underworldSkillTable;
    private readonly List<string> _allUnderworldSkillOptions;

    private string _itemIdText = "";
    private string _transmogIdText = "";
    private string _levelText = "";
    private string _equipPlusValueText = "";
    private string _qualityText = "";
    private string _underworldSkillIdText = "";
    private IReadOnlyList<string> _filteredUnderworldSkillOptions = Array.Empty<string>();
    private bool _isUnderworldSkillDropDownOpen;
    private string? _selectedUnderworldSkillOption;
    private string _familiarityText = "";
    private bool _isUnderworld;

    public EquipmentViewModel(UnderworldSkillTable underworldSkillTable)
    {
        _underworldSkillTable = underworldSkillTable ?? throw new ArgumentNullException(nameof(underworldSkillTable));
        _allUnderworldSkillOptions = _underworldSkillTable.Options.ToList();
        _filteredUnderworldSkillOptions = _allUnderworldSkillOptions;
    }

    public string ItemIdText
    {
        get => _itemIdText;
        set => SetProperty(ref _itemIdText, value);
    }

    public string TransmogIdText
    {
        get => _transmogIdText;
        set => SetProperty(ref _transmogIdText, value);
    }

    public string LevelText
    {
        get => _levelText;
        set => SetProperty(ref _levelText, value);
    }

    public string QualityText
    {
        get => _qualityText;
        set => SetProperty(ref _qualityText, value);
    }

    public string EquipPlusValueText
    {
        get => _equipPlusValueText;
        set => SetProperty(ref _equipPlusValueText, value);
    }

    public string UnderworldSkillIdText
    {
        get => _underworldSkillIdText;
        set
        {
            if (SetProperty(ref _underworldSkillIdText, value))
            {
                if (_selectedUnderworldSkillOption is not null
                    && !string.Equals(_selectedUnderworldSkillOption, value, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedUnderworldSkillOption = null;
                    OnPropertyChanged(nameof(SelectedUnderworldSkillOption));
                }

                UpdateFilteredUnderworldSkillOptions(value);
            }
        }
    }

    public IReadOnlyList<string> FilteredUnderworldSkillOptions
    {
        get => _filteredUnderworldSkillOptions;
        private set => SetProperty(ref _filteredUnderworldSkillOptions, value);
    }

    public bool IsUnderworldSkillDropDownOpen
    {
        get => _isUnderworldSkillDropDownOpen;
        set => SetProperty(ref _isUnderworldSkillDropDownOpen, value);
    }

    public string? SelectedUnderworldSkillOption
    {
        get => _selectedUnderworldSkillOption;
        set
        {
            if (SetProperty(ref _selectedUnderworldSkillOption, value) && !string.IsNullOrWhiteSpace(value))
            {
                UnderworldSkillIdText = value;
                IsUnderworldSkillDropDownOpen = false;
            }
        }
    }

    public string FamiliarityText
    {
        get => _familiarityText;
        set => SetProperty(ref _familiarityText, value);
    }

    public bool IsUnderworld
    {
        get => _isUnderworld;
        set => SetProperty(ref _isUnderworld, value);
    }

    public bool TryToData(out EquipmentData data, out string? error)
    {
        error = null;
        data = default!;

        if (!short.TryParse(ItemIdText, out var itemId))
        {
            error = "Invalid ItemId.";
            return false;
        }

        if (!short.TryParse(TransmogIdText, out var transmogId))
        {
            error = "Invalid TransmogId.";
            return false;
        }

        if (!short.TryParse(LevelText, out var level) || level < 0)
        {
            error = "Invalid Level.";
            return false;
        }

        if (!byte.TryParse(EquipPlusValueText, out var equipPlusValue))
        {
            error = "Invalid EquipPlusValue.";
            return false;
        }

        if (!int.TryParse(QualityText, out var quality) || quality < 0)
        {
            error = "Invalid Quality.";
            return false;
        }

        if (!_underworldSkillTable.TryResolveToId(UnderworldSkillIdText, out var underworldSkillId))
        {
            error = "Invalid UnderworldSkillId.";
            return false;
        }

        if (!int.TryParse(FamiliarityText, out var familiarity) || familiarity < 0)
        {
            error = "Invalid Familiarity.";
            return false;
        }

        data = new EquipmentData(
            itemId,
            transmogId,
            level,
            equipPlusValue,
            quality,
            underworldSkillId,
            familiarity,
            IsUnderworld
        );
        return true;
    }

    private void UpdateFilteredUnderworldSkillOptions(string? input)
    {
        if (ShouldBypassTextSearch(input))
        {
            FilteredUnderworldSkillOptions = _allUnderworldSkillOptions;
            IsUnderworldSkillDropDownOpen = false;
            return;
        }

        var keyword = input!.Trim();
        var filtered = _allUnderworldSkillOptions
            .Where(option => option.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        FilteredUnderworldSkillOptions = filtered;

        var isExactOption = _allUnderworldSkillOptions.Any(option => option.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        IsUnderworldSkillDropDownOpen = !isExactOption && filtered.Count > 0;
    }

    private static bool ShouldBypassTextSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var trimmed = input.Trim();
        if (trimmed == "-")
        {
            return true;
        }

        return int.TryParse(trimmed, out _);
    }

    private void ResetUnderworldSkillOptions()
    {
        FilteredUnderworldSkillOptions = _allUnderworldSkillOptions;
        IsUnderworldSkillDropDownOpen = false;
        SelectedUnderworldSkillOption = null;
    }

    public void SetFromData(EquipmentData data)
    {
        ItemIdText = data.ItemId.ToString();
        TransmogIdText = data.TransmogId.ToString();
        LevelText = data.Level.ToString();
        EquipPlusValueText = data.EquipPlusValue.ToString();
        QualityText = data.Quality.ToString();
        UnderworldSkillIdText = _underworldSkillTable.FormatForDisplay(data.UnderworldSkillId);
        ResetUnderworldSkillOptions();
        FamiliarityText = data.Familiarity.ToString();
        IsUnderworld = data.IsUnderworld;
    }
}
