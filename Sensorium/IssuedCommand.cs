namespace Sensorium
{
    using System;
    using System.Linq;

    /// <summary>
    /// A command that was actually sent to a destination device based on a matching 
    /// behavior (or direct user intervention).
    /// </summary>
    /// <remarks>
    /// Note that there are <see cref="ICommand"/> objects that may pass through the 
    /// event stream that may not reach a device ultimately and therefore never 
    /// become an issued command (i.e. the matching behavior calls for a state 
    /// change when the given device is already in that given state).
    /// </remarks>
    public class IssuedCommand
    {
        public IssuedCommand(ICommand command, string reason)
        {
            this.Command = command;
            this.Reason = reason;
        }

        public string Reason { get; private set; }
        public ICommand Command { get; private set; }

        public override string ToString()
        {
            return Command.ToString() + " (" + Reason + ")";
        }
    }
}