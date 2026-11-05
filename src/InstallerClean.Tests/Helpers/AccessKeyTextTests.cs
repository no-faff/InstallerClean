using System.Collections.Generic;
using System.Globalization;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using Xunit;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The confirmations' Delete permanently and Move buttons show their label through
/// <see cref="AccessKeyText.Remove"/>, so the letter that opens a confirmation from
/// the main window has nothing to press inside it.
/// </summary>
public class AccessKeyTextTests
{
    [Theory]
    [InlineData("_Delete permanently", "Delete permanently")]
    [InlineData("Endgültig _löschen", "Endgültig löschen")]
    [InlineData("Ver_plaatsen", "Verplaatsen")]
    [InlineData("完全に削除(_D)", "完全に削除")]
    [InlineData("Delete (_D)", "Delete")]
    [InlineData("移動（_M）", "移動")]
    [InlineData("No marker", "No marker")]
    [InlineData("", "")]
    // A doubled underscore is a literal one and marks nothing, so it stays doubled
    // for the presenter to draw as one underscore.
    [InlineData("a__b", "a__b")]
    [InlineData("___X", "__X")]
    [InlineData("__a_b", "__ab")]
    // A second single underscore after the marker is doubled, so it draws as it
    // stands and cannot become a key of its own.
    [InlineData("_a_b", "a__b")]
    // A trailing underscore marks nothing, so the label comes back as it was.
    [InlineData("ab_", "ab_")]
    public void Takes_the_marker_out(string label, string expected)
    {
        Assert.Equal(expected, AccessKeyText.Remove(label));
    }

    public static TheoryData<string> Cultures()
    {
        var data = new TheoryData<string>();
        foreach (var name in SupportedLanguages.CultureNames) data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Each_confirmation_label_carries_no_key_in_any_language(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var faults = new List<string>();

        foreach (var key in new[] { "Action.DeletePermanently", "Action.Move" })
        {
            var label = Strings.ResourceManager.GetString(key, culture);
            Assert.False(string.IsNullOrEmpty(label), $"{cultureName}: {key} did not resolve");

            // The label as shipped carries a key; a language without one would make
            // this test pass for the wrong reason.
            if (!label!.Contains('_'))
                faults.Add($"{key} carries no access key to take out: \"{label}\"");

            var shown = AccessKeyText.Remove(label);
            if (shown.Contains('_'))
                faults.Add($"{key} still carries an underscore: \"{shown}\"");
            if (shown.Contains('(') || shown.Contains(')') || shown.Contains('（') || shown.Contains('）'))
                faults.Add($"{key} keeps a bracket: \"{shown}\"");
            if (shown != shown.Trim())
                faults.Add($"{key} keeps a space at an end: \"{shown}\"");
            if (shown.Length == 0)
                faults.Add($"{key} is left empty");
        }

        Assert.True(faults.Count == 0, $"{cultureName}: {string.Join("; ", faults)}");
    }
}
