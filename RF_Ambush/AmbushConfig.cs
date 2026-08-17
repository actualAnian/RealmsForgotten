namespace RF_Ambush;

/// <summary>
/// Numeros do sistema de emboscada, num lugar so.
///
/// Filosofia (decisao do autor, 2026-08-03, ver ESTUDO_TOTALAMBUSH.md): a chance
/// de emboscar NAO e um d100 escondido num menu — e o proprio sistema continuo
/// de visibilidade do jogo (quem ve quem primeiro). Estes numeros calibram esse
/// sistema, nao o substituem.
/// </summary>
public static class AmbushConfig
{
    // ---------- STANCE (campanha, fase 1) ----------

    /// <summary>
    /// Tecla que arma a emboscada no mapa. X porque o mapa vanilla ja ocupa
    /// WASD/setas (mover), Q/E (girar camera), 1/2/3/Espaco (tempo), F5
    /// (quicksave), Shift (rapido) e os paineis (P I C K J N B...), e o RF usa
    /// V e Q — X esta livre nos dois. ("X marks the spot.")
    /// </summary>
    public const TaleWorlds.InputSystem.InputKey StanceKey = TaleWorlds.InputSystem.InputKey.X;

    /// <summary>
    /// Multiplicador de dificuldade-de-ser-visto enquanto a party esta armada em
    /// emboscada. Aplica EM CIMA da dificuldade vanilla (que ja considera floresta
    /// e party parada). A escala vanilla e ~0.25 campo aberto / 0.30 floresta, e a
    /// visibilidade decide por range²/dist²/dificuldade >= 1 — dobrar a dificuldade
    /// corta a distancia de avistamento por ~1.41x.
    /// </summary>
    public const float StanceConcealmentMultiplier = 3.0f;

    /// <summary>Bonus de ocultacao por ponto de Scouting do NOSSO batedor (fracao).</summary>
    public const float ConcealmentPerScoutSkill = 1f / 300f;

    /// <summary>
    /// Homens alem deste tanto comecam a estragar a ocultacao. 20 homens se
    /// escondem numa mata; 200 nao.
    /// </summary>
    public const int ConcealmentSizeGraceMen = 20;

    /// <summary>Horas paradas para atingir a ocultacao plena (assentar poeira, apagar fogueiras).</summary>
    public const float FullConcealmentAfterHours = 3f;

    /// <summary>Fracao da ocultacao da stance disponivel logo ao armar (o resto vem com o tempo).</summary>
    public const float InitialConcealmentFraction = 0.6f;

    /// <summary>Distancia (unidades de mapa) para o bote — o menu de atacar aparece.</summary>
    public const float PounceRange = 3.2f;

    /// <summary>So consideramos presas ate esta distancia (economia do tick).</summary>
    public const float ScanRange = 14f;

    /// <summary>Depois de saltar uma emboscada, este tempo ate poder armar outra.</summary>
    public const float CooldownHours = 8f;

    /// <summary>Moral comeca a cair depois deste tempo emboscado (esperar cansa).</summary>
    public const float MoraleDecayAfterHours = 24f;

    /// <summary>Moral por hora alem do limite acima.</summary>
    public const float MoralePerHourBeyond = 0.5f;

    /// <summary>XP de Scouting por hora oculto com inimigo passando perto sem nos ver.</summary>
    public const float ScoutXpPerStealthHour = 25f;

    /// <summary>XP de Tactics para o jogador ao saltar a emboscada.</summary>
    public const float TacticsXpOnSpring = 150f;

    // ---------- IA EMBOSCA (campanha, fase 2) ----------

    /// <summary>
    /// Chance por hora de uma party elegivel (hostil, invisivel para nos, scout
    /// melhor, terreno bom) decidir nos emboscar. A "chance real" continua sendo
    /// a visibilidade — isto so evita que TODA party elegivel salte sempre.
    /// </summary>
    public const float EnemyAmbushChancePerHour = 0.35f;

    /// <summary>Distancia em que a IA salta sobre o jogador.</summary>
    public const float EnemyPounceRange = 2.6f;

    /// <summary>
    /// CONTRA-JOGO (obrigatorio — sem ele a fase 2 e frustracao): se o nosso
    /// avistamento quase alcancou a party oculta (fracao da visibilidade em
    /// [este valor, 1)), o jogador recebe "sinais de emboscada a frente".
    /// </summary>
    public const float ForewarnVisibilityFraction = 0.55f;

    /// <summary>XP de Scouting pro nosso batedor quando os sinais nos salvam.</summary>
    public const float ScoutXpOnForewarn = 60f;

    // ---------- MISSAO (batalha de emboscada) ----------

    /// <summary>Colunas da formacao de marcha inimiga.</summary>
    public const int MarchColumns = 4;

    /// <summary>Espacamento lateral entre colunas (m).</summary>
    public const float MarchLateralSpacing = 2.2f;

    /// <summary>Espacamento entre fileiras (m).</summary>
    public const float MarchRowSpacing = 2.6f;

    /// <summary>Limite de velocidade dos que marcham (multiplicador de velocidade).</summary>
    public const float MarchSpeedMultiplier = 0.65f;

    /// <summary>Visao frontal de quem marcha desatento (m).</summary>
    public const float MarchForwardVision = 42f;

    /// <summary>Cosseno do meio-cone frontal (0.55 ≈ 56 graus para cada lado).</summary>
    public const float MarchVisionConeCos = 0.55f;

    /// <summary>Percepcao lateral/traseira de quem marcha (m) — esbarrou, acabou.</summary>
    public const float MarchSideAwareness = 14f;

    /// <summary>Intervalo do teste de deteccao na missao (s).</summary>
    public const float DetectionInterval = 0.5f;

    /// <summary>Choque de moral no lado emboscado quando a emboscada salta.</summary>
    public const float SpringMoraleShock = 22f;

    /// <summary>Validade do gatilho armado na campanha (h) — se a batalha nao abrir, desarma.</summary>
    public const float ArmedExpiryHours = 2f;
}
