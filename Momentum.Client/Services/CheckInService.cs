using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class CheckInService(HttpClient http)
{
    // Tag CheckedInAt/CreatedAt as UTC after deserialization so ToLocalTime() in
    // components always has correct kind information (mirrors ActivityLogService).
    private static CheckInDto? TagUtc(CheckInDto? c)
    {
        if (c is not null)
        {
            c.CheckedInAt = DateTime.SpecifyKind(c.CheckedInAt, DateTimeKind.Utc);
            c.CreatedAt   = DateTime.SpecifyKind(c.CreatedAt,   DateTimeKind.Utc);
        }
        return c;
    }

    /// <summary>
    /// Creates a Check-In. Returns the created DTO, or null on any failure
    /// (network, validation, auth) so callers can show a friendly message.
    /// </summary>
    public async Task<CheckInDto?> CreateAsync(CreateCheckInRequestDto dto)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/checkins", dto);
            if (!response.IsSuccessStatusCode) return null;
            return TagUtc(await response.Content.ReadFromJsonAsync<CheckInDto>());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the most recent Check-In for the current user (by CheckedInAt),
    /// or null if none exists. Used to preload the form with the last recorded state.
    /// </summary>
    public async Task<CheckInDto?> GetMostRecentAsync()
    {
        try
        {
            // The API orders by CheckedInAt descending; a wide window captures all
            // history so the first result is the most recent check-in.
            var from = DateTime.UtcNow.AddYears(-5);
            var to   = DateTime.UtcNow.AddDays(1);
            var response = await http.GetAsync($"api/checkins?from={from:O}&to={to:O}");
            if (!response.IsSuccessStatusCode) return null;
            var list = await response.Content.ReadFromJsonAsync<List<CheckInDto>>() ?? [];
            return list.Count > 0 ? TagUtc(list[0]) : null;
        }
        catch
        {
            return null;
        }
    }
}
