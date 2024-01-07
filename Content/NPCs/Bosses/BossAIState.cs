using NoxusBoss.Common.StateMachines;

namespace NoxusBoss.Content.NPCs.Bosses
{
    public class BossAIState<T> : IState<T> where T : struct
    {
        public T Identifier
        {
            get;
            set;
        }

        public int Time;

        public BossAIState(T identifier)
        {
            Identifier = identifier;
        }

        public void OnPoppedFromStack()
        {
            Time = 0;
        }
    }
}
