namespace Sensorium
{
    using System;
    using System.Collections.Generic;

    public class CommandContext
    {
        public CommandContext(string behavior, List<ICommand> undoCommands)
        {
            this.Behavior = behavior;
            this.UndoCommands = undoCommands;
        }

        public string Behavior { get; private set; }
        public List<ICommand> UndoCommands { get; private set; }
    }
}