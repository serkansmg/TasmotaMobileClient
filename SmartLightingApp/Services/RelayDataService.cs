using SmartLightingApp.Models;
using System.Text.Json;

namespace SmartLightingApp.Services;

public class RelayDataService
{
    private const string BOARDS_KEY = "relay_boards_data";
    private const string LAST_UPDATE_KEY = "last_update_timestamp";
    private List<RelayBoard> _boards = new();
    private bool _isInitialized = false;

    public RelayDataService()
    {
        Initialize();
    }

    #region Initialization

    private void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            LoadBoards();
            
            // İlk çalıştırmada veri yoksa dummy data yükle
            if (!_boards.Any())
            {
                LoadDefaultData();
                SaveBoards();
            }
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RelayDataService initialization error: {ex.Message}");
            LoadDefaultData();
            _isInitialized = true;
        }
    }

    private void LoadDefaultData()
    {
        //_boards = DummyDataService.GetDummyBoards();
        _boards=new List<RelayBoard>();
    }

    #endregion

    #region Board Operations

    public async Task<List<RelayBoard>> GetAllBoardsAsync()
    {
        await Task.CompletedTask;
        return new List<RelayBoard>(_boards);
    }

    public async Task<RelayBoard?> GetBoardByIdAsync(int boardId)
    {
        await Task.CompletedTask;
        return _boards.FirstOrDefault(b => b.Id == boardId);
    }

    public async Task<RelayBoard> AddBoardAsync(RelayBoard board)
    {
        // Yeni ID ata
        board.Id = _boards.Any() ? _boards.Max(b => b.Id) + 1 : 1;
        board.LastSeen = DateTime.Now;
        
        _boards.Add(board);
        SaveBoards();
        
        await Task.CompletedTask;
        return board;
    }

    public async Task<bool> UpdateBoardAsync(RelayBoard board)
    {
        var existingBoard = _boards.FirstOrDefault(b => b.Id == board.Id);
        if (existingBoard == null) return false;

        var index = _boards.IndexOf(existingBoard);
        board.LastSeen = DateTime.Now;
        _boards[index] = board;
        
        SaveBoards();
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> DeleteBoardAsync(int boardId)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return false;

        _boards.Remove(board);
        SaveBoards();
        
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> UpdateBoardNameAsync(int boardId, string newName)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return false;

        board.Name = newName;
        board.LastSeen = DateTime.Now;
        SaveBoards();
        
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> UpdateBoardOnlineStatusAsync(int boardId, bool isOnline)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return false;

        board.IsOnline = isOnline;
        board.LastSeen = DateTime.Now;
        SaveBoards();
        
        await Task.CompletedTask;
        return true;
    }

    #endregion

    #region Relay Operations

    public async Task<bool> UpdateRelayStateAsync(int boardId, int relayNumber, bool isOn)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return false;

        var relay = board.Relays.FirstOrDefault(r => r.RelayNumber == relayNumber);
        if (relay == null) return false;

        relay.IsOn = isOn;
        relay.LastToggled = DateTime.Now;
        board.LastSeen = DateTime.Now;
        
        SaveBoards();
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> UpdateRelayNameAsync(int boardId, int relayNumber, string newName)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return false;

        var relay = board.Relays.FirstOrDefault(r => r.RelayNumber == relayNumber);
        if (relay == null) return false;

        relay.Name = newName;
        board.LastSeen = DateTime.Now;
        
        SaveBoards();
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> UpdateRelayTimerAsync(int boardId, int relayNumber, bool hasTimer)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return false;

        var relay = board.Relays.FirstOrDefault(r => r.RelayNumber == relayNumber);
        if (relay == null) return false;

        relay.HasTimer = hasTimer;
        board.LastSeen = DateTime.Now;
        
        SaveBoards();
        await Task.CompletedTask;
        return true;
    }

    public async Task<RelayItem?> GetRelayAsync(int boardId, int relayNumber)
    {
        var board = _boards.FirstOrDefault(b => b.Id == boardId);
        if (board == null) return null;

        await Task.CompletedTask;
        return board.Relays.FirstOrDefault(r => r.RelayNumber == relayNumber);
    }

    #endregion

    #region Statistics

    public async Task<(int TotalBoards, int OnlineBoards, int TotalRelays, int ActiveRelays)> GetStatisticsAsync()
    {
        var totalBoards = _boards.Count;
        var onlineBoards = _boards.Count(b => b.IsOnline);
        var totalRelays = _boards.SelectMany(b => b.Relays).Count();
        var activeRelays = _boards.SelectMany(b => b.Relays).Count(r => r.IsOn);

        await Task.CompletedTask;
        return (totalBoards, onlineBoards, totalRelays, activeRelays);
    }

    public async Task<List<RelayBoard>> GetOnlineBoardsAsync()
    {
        await Task.CompletedTask;
        return _boards.Where(b => b.IsOnline).ToList();
    }

    public async Task<List<RelayBoard>> GetOfflineBoardsAsync()
    {
        await Task.CompletedTask;
        return _boards.Where(b => !b.IsOnline).ToList();
    }

    #endregion

    #region Data Persistence

    private void LoadBoards()
    {
        try
        {
            var jsonData = Preferences.Get(BOARDS_KEY, string.Empty);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                
                var boards = JsonSerializer.Deserialize<List<RelayBoard>>(jsonData, options);
                _boards = boards ?? new List<RelayBoard>();
            }
            else
            {
                _boards = new List<RelayBoard>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading boards: {ex.Message}");
            _boards = new List<RelayBoard>();
        }
    }

    private void SaveBoards()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            var jsonData = JsonSerializer.Serialize(_boards, options);
            Preferences.Set(BOARDS_KEY, jsonData);
            Preferences.Set(LAST_UPDATE_KEY, DateTime.Now.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving boards: {ex.Message}");
        }
    }

    public async Task SaveAllDataAsync()
    {
        SaveBoards();
        await Task.CompletedTask;
    }

    public async Task<RelayBoard?> FindAndUpdateByHostnameOrMacAsync(string hostname, string macAddress, string newIpAddress)
    {
        var existingBoard = _boards.FirstOrDefault(b => 
            (!string.IsNullOrEmpty(b.Hostname) && b.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(b.MacAddress) && b.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase))
        );

        if (existingBoard != null)
        {
            // Sadece IP, Hostname, Mac ve LastSeen güncelle - Name ve Relays'e dokunma
            existingBoard.IpAddress = newIpAddress;
            existingBoard.Hostname = hostname;
            existingBoard.MacAddress = macAddress;
            existingBoard.LastSeen = DateTime.Now;
            existingBoard.IsOnline = true;
        
            SaveBoards();
            await Task.CompletedTask;
            return existingBoard;
        }
    
        await Task.CompletedTask;
        return null;
    }
    
    public async Task<DateTime?> GetLastUpdateTimeAsync()
    {
        var lastUpdateStr = Preferences.Get(LAST_UPDATE_KEY, string.Empty);
        
        if (DateTime.TryParse(lastUpdateStr, out var lastUpdate))
        {
            await Task.CompletedTask;
            return lastUpdate;
        }
        
        await Task.CompletedTask;
        return null;
    }

    #endregion

    #region Data Management

    public async Task ClearAllDataAsync()
    {
        _boards.Clear();
        Preferences.Remove(BOARDS_KEY);
        Preferences.Remove(LAST_UPDATE_KEY);
        await Task.CompletedTask;
    }

    public async Task ResetToDefaultDataAsync()
    {
        _boards.Clear();
        LoadDefaultData();
        SaveBoards();
        await Task.CompletedTask;
    }

    public async Task<string> ExportDataAsync()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        
        await Task.CompletedTask;
        return JsonSerializer.Serialize(_boards, options);
    }

    public async Task<bool> ImportDataAsync(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            var boards = JsonSerializer.Deserialize<List<RelayBoard>>(jsonData, options);
            if (boards != null)
            {
                _boards = boards;
                SaveBoards();
                await Task.CompletedTask;
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing data: {ex.Message}");
        }
        
        await Task.CompletedTask;
        return false;
    }

    #endregion
}