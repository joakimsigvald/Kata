namespace Pool.Services;
public interface IWaterTap
{
    bool IsOpen { get; }

    void Open();
    void Close();
}