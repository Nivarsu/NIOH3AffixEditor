using System;
using System.Collections.Generic;
using System.Linq;
using Nioh3AffixEditor.Infrastructure;
using Nioh3AffixEditor.Models;
using Nioh3AffixEditor.Services;

namespace Nioh3AffixEditor.ViewModels;

public sealed class AffixSlotViewModel : ObservableObject
{
    private readonly AffixIdTable _affixIdTable;
    private readonly List<string> _allAffixIdOptions;

    private string _affixIdText = "";
    private IReadOnlyList<string> _filteredAffixIdOptions = Array.Empty<string>();
    private bool _isAffixIdDropDownOpen;
    private string? _selectedAffixIdOption;
    private string _levelText = "";
    private string _prefix1Text = "";
    private string _prefix2Text = "";
    private string _prefix3Text = "";
    private string _prefix4Text = "";
    private bool _isApplyingSelection;

    public AffixSlotViewModel(AffixIdTable affixIdTable, int slotIndex)
    {
        _affixIdTable = affixIdTable ?? throw new ArgumentNullException(nameof(affixIdTable));
        _allAffixIdOptions = _affixIdTable.Options.ToList();
        _filteredAffixIdOptions = _allAffixIdOptions;
        SlotIndex = slotIndex;
    }

    public int SlotIndex { get; }

    public string AffixIdText
    {
        get => _affixIdText;
        set
        {
            if (SetProperty(ref _affixIdText, value))
            {
                if (!_isApplyingSelection
                    && _selectedAffixIdOption is not null
                    && !IsSameAffixSelection(_selectedAffixIdOption, value))
                {
                    _selectedAffixIdOption = null;
                    OnPropertyChanged(nameof(SelectedAffixIdOption));
                }

                UpdateFilteredAffixOptions(value);
            }
        }
    }

    public IReadOnlyList<string> FilteredAffixIdOptions
    {
        get => _filteredAffixIdOptions;
        private set => SetProperty(ref _filteredAffixIdOptions, value);
    }

    public bool IsAffixIdDropDownOpen
    {
        get => _isAffixIdDropDownOpen;
        set => SetProperty(ref _isAffixIdDropDownOpen, value);
    }

    public string? SelectedAffixIdOption
    {
        get => _selectedAffixIdOption;
        set
        {
            if (SetProperty(ref _selectedAffixIdOption, value) && !string.IsNullOrWhiteSpace(value))
            {
                _isApplyingSelection = true;
                try
                {
                    if (_affixIdTable.TryResolveToId(value, out var affixId))
                    {
                        AffixIdText = _affixIdTable.FormatForDisplay(affixId);
                    }
                    else
                    {
                        AffixIdText = value;
                    }
                    IsAffixIdDropDownOpen = false;
                }
                finally
                {
                    _isApplyingSelection = false;
                }
            }
        }
    }

    public string LevelText
    {
        get => _levelText;
        set => SetProperty(ref _levelText, value);
    }

    public string Prefix1Text
    {
        get => _prefix1Text;
        set => SetProperty(ref _prefix1Text, value);
    }

    public string Prefix2Text
    {
        get => _prefix2Text;
        set => SetProperty(ref _prefix2Text, value);
    }

    public string Prefix3Text
    {
        get => _prefix3Text;
        set => SetProperty(ref _prefix3Text, value);
    }

    public string Prefix4Text
    {
        get => _prefix4Text;
        set => SetProperty(ref _prefix4Text, value);
    }

    public void NormalizeEmptyInputs()
    {
        if (string.IsNullOrWhiteSpace(AffixIdText))
        {
            AffixIdText = "FFFFFFFF";
        }

        if (string.IsNullOrWhiteSpace(LevelText))
        {
            LevelText = "0";
        }

        if (string.IsNullOrWhiteSpace(Prefix1Text)) Prefix1Text = "0";
        if (string.IsNullOrWhiteSpace(Prefix2Text)) Prefix2Text = "0";
        if (string.IsNullOrWhiteSpace(Prefix3Text)) Prefix3Text = "0";
        if (string.IsNullOrWhiteSpace(Prefix4Text)) Prefix4Text = "0";
    }

    private void UpdateFilteredAffixOptions(string? input)
    {
        if (ShouldBypassTextSearch(input))
        {
            FilteredAffixIdOptions = _allAffixIdOptions;
            IsAffixIdDropDownOpen = false;
            return;
        }

        var keyword = input!.Trim();
        var filtered = _allAffixIdOptions
            .Where(option => option.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        FilteredAffixIdOptions = filtered;

        var isExactOption = _allAffixIdOptions.Any(option => option.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        IsAffixIdDropDownOpen = !isExactOption && filtered.Count > 0;
    }

    private static bool ShouldBypassTextSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var trimmed = input.Trim();
        if (trimmed.Equals("FFFFFFFF", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trimmed.All(c => c is 'F' or 'f') && trimmed.Length <= 8)
        {
            return true;
        }

        if (trimmed == "-")
        {
            return true;
        }

        if (LooksLikeDisplayWithId(trimmed))
        {
            return true;
        }

        return int.TryParse(trimmed, out _);
    }

    private bool IsSameAffixSelection(string selectedOption, string currentText)
    {
        if (string.Equals(selectedOption, currentText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!_affixIdTable.TryResolveToId(selectedOption, out var selectedId))
        {
            return false;
        }

        if (!_affixIdTable.TryResolveToId(currentText, out var currentId))
        {
            return false;
        }

        return selectedId == currentId;
    }

    private static bool LooksLikeDisplayWithId(string text)
    {
        int open;
        int close;

        var openAscii = text.LastIndexOf('(');
        var closeAscii = text.LastIndexOf(')');
        if (openAscii >= 0 && closeAscii > openAscii)
        {
            open = openAscii;
            close = closeAscii;
        }
        else
        {
            var openWide = text.LastIndexOf('（');
            var closeWide = text.LastIndexOf('）');
            if (openWide < 0 || closeWide <= openWide)
            {
                return false;
            }

            open = openWide;
            close = closeWide;
        }

        var idPart = text.Substring(open + 1, close - open - 1).Trim();
        return int.TryParse(idPart, out _);
    }

    private void ResetAffixOptions()
    {
        FilteredAffixIdOptions = _allAffixIdOptions;
        IsAffixIdDropDownOpen = false;
        SelectedAffixIdOption = null;
    }

    private bool TryParseAffixId(string? text, out int affixId)
    {
        return _affixIdTable.TryResolveToId(text, out affixId);
    }

    private static bool TryParseLevel(string? text, out int level)
    {
        level = 0;
        if (string.IsNullOrWhiteSpace(text)) return true;
        return int.TryParse(text.Trim(), out level);
    }

    private static bool TryParsePrefixByte(string? text, out byte value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return true;
        return byte.TryParse(text.Trim(), out value);
    }

    public bool TryToData(out AffixSlotData data, out string? error)
    {
        error = null;
        data = default!;

        NormalizeEmptyInputs();

        if (!TryParseAffixId(AffixIdText, out var affixId))
        {
            error = $"Slot {SlotIndex}: invalid affix ID.";
            return false;
        }

        if (affixId < -1)
        {
            error = $"Slot {SlotIndex}: invalid affix ID.";
            return false;
        }

        if (!TryParseLevel(LevelText, out var level) || level < 0)
        {
            error = $"Slot {SlotIndex}: invalid level.";
            return false;
        }

        if (!TryParsePrefixByte(Prefix1Text, out var prefix1)
            || !TryParsePrefixByte(Prefix2Text, out var prefix2)
            || !TryParsePrefixByte(Prefix3Text, out var prefix3)
            || !TryParsePrefixByte(Prefix4Text, out var prefix4))
        {
            error = $"Slot {SlotIndex}: invalid prefix (0..255).";
            return false;
        }

        data = new AffixSlotData(SlotIndex, affixId, level, prefix1, prefix2, prefix3, prefix4);
        return true;
    }

    public void SetFromData(AffixSlotData data)
    {
        ResetAffixOptions();
        AffixIdText = _affixIdTable.FormatForDisplay(data.AffixId);
        LevelText = data.Level.ToString();
        Prefix1Text = data.Prefix1.ToString();
        Prefix2Text = data.Prefix2.ToString();
        Prefix3Text = data.Prefix3.ToString();
        Prefix4Text = data.Prefix4.ToString();
    }
}
