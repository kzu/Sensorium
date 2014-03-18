namespace Sensorium
{
    using System;
    using System.Linq;
    using System.Reactive;
    using Sensorium.Properties;

    public static class CommandExtensions
    {
        public static IDo ToDo<T>(this ICommand<T> command)
        {
            if (typeof(T) == typeof(bool))
                return ((ICommand<bool>)command).ToDo();
            else if (typeof(T) == typeof(float))
                return ((ICommand<float>)command).ToDo();
            else if (typeof(T) == typeof(string))
                return ((ICommand<string>)command).ToDo();
            else if (typeof(T) == typeof(Unit))
                return ((ICommand<Unit>)command).ToDo();

            throw new NotSupportedException(Strings.CommandExtensions.CannotConvertToDo(typeof(T)));
        }

        public static IDo ToDo(this ICommand<bool> command)
        {
            return new Do(command.Topic, Payload.ToBytes(command.Payload));
        }

        public static IDo ToDo(this ICommand<float> command)
        {
            return new Do(command.Topic, Payload.ToBytes(command.Payload));
        }

        public static IDo ToDo(this ICommand<string> command)
        {
            return new Do(command.Topic, Payload.ToBytes(command.Payload));
        }

        public static IDo ToDo(this ICommand<Unit> command)
        {
            return new Do(command.Topic, new byte[0]);
        }
    }
}