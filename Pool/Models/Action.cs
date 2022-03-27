namespace Pool.Models
{
    public struct Action
    {
        public DateTime TimeStamp { get; init; }
        public Command Command { get; init; }
    }
}