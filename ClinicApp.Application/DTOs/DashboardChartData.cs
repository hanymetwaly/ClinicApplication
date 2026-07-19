namespace ClinicApp.Application.DTOs;

public class DashboardChartData
{
    public ChartSeries Appointments { get; set; } = new();
    public ChartSeries Revenue { get; set; } = new();
}

public class ChartSeries
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Data { get; set; } = new();
}
