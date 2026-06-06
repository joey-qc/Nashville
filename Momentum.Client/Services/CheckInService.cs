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

    private static List<CheckInDto> TagUtc(List<CheckInDto> list)
    {
        foreach (var c in list)
        {
            c.CheckedInAt = DateTime.SpecifyKind(c.CheckedInAt, DateTimeKind.Utc);
            c.CreatedAt   = DateTime.SpecifyKind(c.CreatedAt,   DateTimeKind.Utc);
        }
        return list;
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
        var all = await GetAllAsync();
        return all.Count > 0 ? all[0] : null;
    }

    /// <summary>
    /// Returns all Check-Ins for the current user, newest first. Backed by the
    /// date-range endpoint with a wide window (no pagination — history is small).
    /// Returns an empty list on failure.
    /// </summary>
    public async Task<List<CheckInDto>> GetAllAsync()
    {
        try
        {
            var from = DateTime.UtcNow.AddYears(-5);
            var to   = DateTime.UtcNow.AddDays(1);
            var response = await http.GetAsync($"api/checkins?from={from:O}&to={to:O}");
            if (!response.IsSuccessStatusCode) return [];
            var list = await response.Content.ReadFromJsonAsync<List<CheckInDto>>() ?? [];
            TagUtc(list);
            // Sort newest-first by the true instant. TagUtc marks each CheckedInAt as UTC,
            // so the comparison is by the actual point in time (not a local/Unspecified mix).
            return list.OrderByDescending(c => c.CheckedInAt).ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Updates a Check-In. Returns the updated DTO, or null on failure.</summary>
    public async Task<CheckInDto?> UpdateAsync(int id, UpdateCheckInRequestDto dto)
    {
        try
        {
            var response = await http.PutAsJsonAsync($"api/checkins/{id}", dto);
            if (!response.IsSuccessStatusCode) return null;
            return TagUtc(await response.Content.ReadFromJsonAsync<CheckInDto>());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Deletes a Check-In. Returns true on success.</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var response = await http.DeleteAsync($"api/checkins/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
