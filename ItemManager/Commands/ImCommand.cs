using Smod2.Commands;

namespace ItemManager.Commands
{
    public abstract class ImCommand : ICommandHandler
    {
        protected ImPlugin Plugin { get; }

        protected abstract string Usage { get; }
        protected abstract string Description { get; }

        protected ImCommand(ImPlugin plugin)
        {
            Plugin = plugin;
        }

        public abstract string[] OnCall(ICommandSender sender, string[] args);

        public string GetUsage() => Usage;

        public string GetCommandDescription() => Description;
    }
}
