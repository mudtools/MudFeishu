namespace FeishuWikiManager.Services;

public class StateStorageService : IStateStorageService
{
    private readonly Dictionary<string, DateTime> _states = new();
    private readonly TimeSpan _expiration;
    private readonly object _lock = new();

    public StateStorageService(TimeSpan expiration)
    {
        _expiration = expiration;
    }

    public string GenerateState()
    {
        var state = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _states[state] = DateTime.UtcNow.Add(_expiration);
        }
        return state;
    }

    public bool ValidateState(string state)
    {
        lock (_lock)
        {
            if (_states.TryGetValue(state, out var expiration))
            {
                if (expiration > DateTime.UtcNow)
                {
                    return true;
                }
                _states.Remove(state);
            }
        }
        return false;
    }

    public void RemoveState(string state)
    {
        lock (_lock)
        {
            _states.Remove(state);
        }
    }

    public void CleanExpiredStates()
    {
        lock (_lock)
        {
            var expiredKeys = _states
                .Where(kvp => kvp.Value <= DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _states.Remove(key);
            }
        }
    }
}
