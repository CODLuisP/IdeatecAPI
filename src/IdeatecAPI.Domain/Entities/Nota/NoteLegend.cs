namespace IdeatecAPI.Domain.Entities;

public class NoteLegend
{
    public int NoteLegendId { get; set; }
    public int ComprobanteId { get; set; }
    public string Code { get; set; } = string.Empty;  // '1000'
    public string Value { get; set; } = string.Empty; // 'SON CIENTO...'
}