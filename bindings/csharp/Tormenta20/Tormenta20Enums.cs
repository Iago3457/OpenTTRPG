namespace OpenTTRPG.Tormenta20
{
    /// <summary>
    /// Os 6 Atributos canônicos de Tormenta20 (Edição Jogo do Ano).
    /// Em T20 JdA, o valor do atributo é diretamente o seu modificador (ex.: +3, +1, 0, -1).
    /// </summary>
    public enum AttributeType
    {
        Strength = 0,     // Força (FOR)
        Dexterity = 1,    // Destreza (DES)
        Constitution = 2, // Constituição (CON)
        Intelligence = 3, // Inteligência (INT)
        Wisdom = 4,       // Sabedoria (SAB)
        Charisma = 5      // Carisma (CAR)
    }

    /// <summary>
    /// Categorias de itens e equipamentos no sistema Tormenta20.
    /// </summary>
    public enum ItemType
    {
        Weapon = 1,        // Armas (corpo a corpo, disparo, arremesso)
        Armor = 2,         // Armaduras leves e pesadas (concedem Defesa e impõem Penalidade)
        Shield = 3,        // Escudos (leves ou pesados)
        Consumable = 4,    // Consumíveis (poções, bálsamos, pergaminhos)
        Accessory = 5,     // Acessórios e vestuário
        Miscellaneous = 6  // Itens gerais e ferramentas
    }

    /// <summary>
    /// Tipos de dano canônicos de Tormenta20 mapeados para DamageTypeId no Core C++.
    /// </summary>
    public enum T20DamageType : int
    {
        Untyped = 0,     // Sem tipo / Dano direto
        Slashing = 1,    // Corte
        Piercing = 2,    // Perfuração
        Bludgeoning = 3, // Impacto
        Fire = 4,        // Fogo
        Cold = 5,        // Frio
        Electricity = 6, // Eletricidade
        Acid = 7,        // Ácido
        Essence = 8,     // Essência (mágico puro)
        Light = 9,       // Luz
        Darkness = 10,   // Trevas
        Psychic = 11,    // Psíquico
        Poison = 12      // Veneno
    }

    /// <summary>
    /// Tipos de ação de Tormenta20 (Edição Jogo do Ano).
    /// Ordem na Fila de Prioridades do turno:
    /// 1º Livre (10) -> 2º Movimento (20) -> 3º Padrão/Comum (30) -> 4º Completa (40).
    /// </summary>
    public enum T20ActionType : int
    {
        Free = 0,     // 0 - Ação Livre (Prioridade 10)
        Movement = 1, // 1 - Ação de Movimento (Prioridade 20)
        Standard = 2, // 2 - Ação Padrão / Comum (Prioridade 30)
        Full = 3,     // 3 - Ação Completa (Consome 1 Padrão + 1 Movimento, Prioridade 40)
        Reaction = 4  // 4 - Reação (Acionada fora/durante turnos por gatilho)
    }

    /// <summary>
    /// Condições canônicas de Tormenta20 (Edição Jogo do Ano).
    /// </summary>
    public enum T20Condition
    {
        Abalado,        // -2 em perícias; se receber Abalado novamente, fica Apavorado
        Apavorado,      // -5 em perícias
        Caido,          // -5 em ataques corpo a corpo, -5 na Defesa corpo a corpo
        Cego,           // Desprevenido e Lento, -5 em perícias de FOR/DES
        Desprevenido,   // -5 na Defesa e -5 em Reflexos
        Enjoado,        // Só pode realizar 1 Ação Padrão OU 1 Ação de Movimento por turno
        Fraco,          // -2 em FOR, DES, CON e perícias baseadas neles
        Imovel,         // Deslocamento reduzido a 0m (bloqueia deslocamento)
        Lento,          // Deslocamento à metade e só 1 Padrão OU 1 Movimento
        Ofuscado,       // -2 em ataques e Percepção
        Sangrando,      // Início do turno: teste CON (CD 15) ou perde 1d6 PV
        Sobrecarregado, // Penalidade de armadura aumenta em -5 e deslocamento -3m
        Vulneravel      // -2 na Defesa
    }

    /// <summary>
    /// Qualidade do descanso entre encontros em Tormenta20.
    /// </summary>
    public enum RestQuality
    {
        Bad = 1,         // Ruim: metade do nível (mínimo 1)
        Normal = 2,      // Normal: igual ao nível
        Comfortable = 3, // Confortável: dobro do nível
        Luxurious = 4    // Luxuoso: triplo do nível
    }
}

