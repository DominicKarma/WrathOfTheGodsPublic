using System;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.MiscSceneManagers
{
    public sealed class ManagedILEdit
    {
        // Due to limitations with events, it's not possible to supply the event itself to this class and apply subscriptions here.
        // Unfortunately, barring reflection this has to be achieved via a premade action.
        private readonly Action<ManagedILEdit> eventApplicationFunction;

        private readonly ManagedILManipulator editingFunction;

        /// <summary>
        /// The name of the IL edit. This serves as an identifier when error logging.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// An alteration of <see cref="ILContext.Manipulator"/> that includes <see cref="ManagedILEdit"/> for logging purposes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="edit"></param>
        public delegate void ManagedILManipulator(ILContext context, ManagedILEdit edit);

        public ManagedILEdit(string name, Action<ManagedILEdit> eventApplicationFunction, ManagedILManipulator editingFunction)
        {
            Name = name;
            this.eventApplicationFunction = eventApplicationFunction;
            this.editingFunction = editingFunction;
        }

        public void SubscriptionWrapper(ILContext context) => editingFunction(context, this);

        /// <summary>
        /// Attempts to apply the IL edit.
        /// </summary>
        /// <param name="onMainThread">Whether the IL edit should be deferred to the main thread or not. This is typically used in the context of drawing based IL edits.</param>
        public void Apply(bool onMainThread = false)
        {
            if (!onMainThread)
            {
                eventApplicationFunction?.Invoke(this);
                return;
            }

            Main.QueueMainThreadAction(() =>
            {
                eventApplicationFunction?.Invoke(this);
            });
        }

        /// <summary>
        /// Provides a standardization for IL editing failure cases, making use of <see cref="Mod.Logger"/>.<br></br>
        /// This should be used if an IL edit could not be loaded for any reason, such as a <see cref="ILCursor.TryGotoNext(MoveType, System.Func{Mono.Cecil.Cil.Instruction, bool}[])"/> failure.
        /// </summary>
        /// <param name="reason">The reason that the IL edit failed.</param>
        public void LogFailure(string reason)
        {
            NoxusBoss.Instance.Logger.Warn($"The IL edit of the name '{Name}' by {NoxusBoss.Instance.DisplayName} failed to load for the following reason:\n{reason}");
        }
    }
}
