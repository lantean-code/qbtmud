namespace Lantean.QBTMud.Models
{
    public enum SearchInScope
    {
        Everywhere = 0,
        Names = 1
    }

    public enum SearchSizeUnit
    {
        Bytes = 0,
        Kibibytes = 1,
        Mebibytes = 2,
        Gibibytes = 3,
        Tebibytes = 4,
        Pebibytes = 5,
        Exbibytes = 6,
    }

    public record SearchFilterOptions(
        string? FilterText,
        SearchInScope SearchIn,
        int? MinimumSeeds,
        int? MaximumSeeds,
        double? MinimumSize,
        SearchSizeUnit MinimumSizeUnit,
        double? MaximumSize,
        SearchSizeUnit MaximumSizeUnit);
}
