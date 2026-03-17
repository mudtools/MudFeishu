namespace FeishuWikiManager.Services;

public interface IStateStorageService
{
    string GenerateState();
    bool ValidateState(string state);
    void RemoveState(string state);
    void CleanExpiredStates();
}
