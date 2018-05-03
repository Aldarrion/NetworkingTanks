using Client.Entities;

namespace Client.Commands
{
    public abstract class Command
    {
        public abstract void Execute(Player player);
    }
}
