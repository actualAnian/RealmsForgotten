# Third-party notices — RF Coop Warsails

## BannerlordCoop (adaptado com permissão)

Partes do design de sincronização de agente deste módulo (seleção de campos de
estado do agente e a lógica de aplicação de movimento — teleporte-se-distante,
direção de movimento/olhar/input) foram **adaptadas** do projeto
**BannerlordCoop** (github.com/Bannerlord-Coop-Team/BannerlordCoop),
especificamente de `Missions/Services/Agents/Packets/{MovementPacket,AgentData}.cs`
e `Missions/Services/CoopArenaController.cs`.

Uso autorizado pelo mantenedor do COOP ("Harlaus / COOP"), que concedeu a
Gustavo e à equipe Realms Forgotten permissão para usar e adaptar código do
repositório do COOP para o Realms Forgotten (declaração pública em 2026-08-15).

> Recomendação em aberto: formalizar essa permissão por escrito num lugar
> durável (issue/PR/e-mail do detentor dos direitos) antes de distribuir
> publicamente. Ver PLANO_RF_COOP_WARSAILS.md.

Este módulo NÃO copia o código-fonte do COOP verbatim; ele reimplementa o
conceito sobre transporte próprio (LiteNetLib direto), sem as dependências de
infraestrutura do COOP (MessageBroker, GameInterface, ProtoBuf surrogates).

## LiteNetLib (MIT)

Transporte de rede. Copyright (c) 2020 Ruslan Pyrch.
Licença MIT — github.com/RevenantX/LiteNetLib. Biblioteca independente, sem
relação com o BannerlordCoop; obtida via NuGet.
