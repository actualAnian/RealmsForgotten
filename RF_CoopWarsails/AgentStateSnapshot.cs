using LiteNetLib.Utils;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_CoopWarsails
{
    /// <summary>
    /// Estado minimo de um agente para sincronizacao de movimento entre os dois
    /// jogadores. A escolha de campos e a logica de Apply (teleporte quando
    /// distante, direcao de movimento/olhar/input) sao ADAPTADAS do
    /// BannerlordCoop (Missions/Services/Agents/Packets/AgentData.cs), usadas
    /// com permissao. Ver THIRD_PARTY_NOTICES.md. Serializacao aqui e propria
    /// (NetDataWriter), sem a pilha ProtoBuf/GameInterface do COOP.
    ///
    /// Marco 2: so movimento (sem equipamento/acao/montaria/dano). Esses vem
    /// nos marcos seguintes.
    /// </summary>
    internal struct AgentStateSnapshot
    {
        public Vec3 Position;
        public Vec2 MovementDirection;
        public Vec3 LookDirection;
        public Vec2 InputVector;
        public float Health;

        public static AgentStateSnapshot Capture(Agent agent)
        {
            return new AgentStateSnapshot
            {
                Position = agent.Position,
                MovementDirection = agent.GetMovementDirection(),
                LookDirection = agent.LookDirection,
                InputVector = agent.MovementInputVector,
                Health = agent.Health,
            };
        }

        /// <summary>
        /// Aplica o estado a um agente fantoche local. Logica adaptada de
        /// AgentData.Apply do BannerlordCoop.
        /// </summary>
        public void Apply(Agent agent)
        {
            if (agent.Health <= 0f) return;

            Vec3 pos = Position;
            if (agent.GetPathDistanceToPoint(ref pos) > 1f)
                agent.TeleportToPosition(pos);

            agent.SetMovementDirection(MovementDirection);
            agent.LookDirection = LookDirection;
            agent.MovementInputVector = InputVector;
        }

        public void Write(NetDataWriter w)
        {
            w.Put(Position.x); w.Put(Position.y); w.Put(Position.z);
            w.Put(MovementDirection.x); w.Put(MovementDirection.y);
            w.Put(LookDirection.x); w.Put(LookDirection.y); w.Put(LookDirection.z);
            w.Put(InputVector.x); w.Put(InputVector.y);
            w.Put(Health);
        }

        public static AgentStateSnapshot Read(NetDataReader r)
        {
            var s = new AgentStateSnapshot();
            s.Position = new Vec3(r.GetFloat(), r.GetFloat(), r.GetFloat());
            s.MovementDirection = new Vec2(r.GetFloat(), r.GetFloat());
            s.LookDirection = new Vec3(r.GetFloat(), r.GetFloat(), r.GetFloat());
            s.InputVector = new Vec2(r.GetFloat(), r.GetFloat());
            s.Health = r.GetFloat();
            return s;
        }
    }
}
