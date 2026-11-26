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
        Inconsciente,   // Indefeso e não pode realizar ações (-10 na Defesa)
        Lento,          // Deslocamento à metade e só 1 Padrão OU 1 Movimento
        Ofuscado,       // -2 em ataques e Percepção
        Sangrando,      // Início do turno: teste CON (CD 15) ou perde 1d6 PV
        Sobrecarregado, // Penalidade de armadura aumenta em -5 e deslocamento -3m
        Vulneravel      // -2 na Defesa
    }

    /// <summary>
    /// Estado vital do personagem segundo as regras de Ferimentos e Morte de Tormenta20 (Jogo do Ano).
    /// </summary>
    public enum T20HealthState
    {
        Healthy = 0,           // Ativo / Saudável (PV > MaxHp / 2)
        Bloodied = 1,          // Machucado (0 < PV <= MaxHp / 2)
        Dying = 2,             // Morrendo: Inconsciente e Sangrando (LimiteDeMorte < PV <= 0, exige CON CD 15 por turno)
        UnconsciousStable = 3, // Inconsciente e Estabilizado (LimiteDeMorte < PV <= 0, sem sangramento)
        Dead = 4               // Morto (PV <= -max(10, MaxHp / 2))
    }

    /// <summary>
    /// Configura quais benefícios se aplicam ao atacar um alvo flanqueado:
    /// Nenhum (para criaturas imunes a flanco), +2 oficial de Tormenta20 JdA, Vantagem (2d20kh1) ou Ambos.
    /// </summary>
    [System.Flags]
    public enum T20FlankingRuleMode : int
    {
        None       = 0,
        BonusPlus2 = 1 << 0,
        Advantage  = 1 << 1,
        Both       = BonusPlus2 | Advantage
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

    /// <summary>
    /// As 14 Classes Canônicas do Livro Básico de Tormenta20 (Edição Jogo do Ano).
    /// </summary>
    public enum T20Class
    {
        Guerreiro = 0,
        Arcanista = 1,
        Barbaro = 2,
        Bardo = 3,
        Bucaneiro = 4,
        Cacador = 5,
        Cavaleiro = 6,
        Clerigo = 7,
        Druida = 8,
        Inventor = 9,
        Ladino = 10,
        Lutador = 11,
        Nobre = 12,
        Paladino = 13
    }

    /// <summary>
    /// As 17 Raças Canônicas do Livro Básico de Tormenta20 (Edição Jogo do Ano).
    /// </summary>
    public enum T20Race
    {
        Humano = 0,
        Anao = 1,
        Dahllan = 2,
        Elfo = 3,
        Goblin = 4,
        Lefou = 5,
        Minotauro = 6,
        Qareen = 7,
        Golem = 8,
        Hynne = 9,
        Kliren = 10,
        Medusa = 11,
        Osteon = 12,
        SereiaTritao = 13,
        Silfide = 14,
        Suraggel = 15,
        Trog = 16
    }

    /// <summary>
    /// Origens Canônicas de Tormenta20 (Edição Jogo do Ano).
    /// </summary>
    public enum T20Origin
    {
        Soldado = 0,
        Acolito = 1,
        AmigoDosAnimais = 2,
        Aristocrata = 3,
        Artesao = 4,
        Artista = 5,
        AssistenteDeLaboratorio = 6,
        Batedor = 7,
        Capanga = 8,
        Charlatao = 9,
        Circense = 10,
        Criminoso = 11,
        Curandeiro = 12,
        Eremita = 13,
        Escravo = 14,
        Estudioso = 15,
        Fazendeiro = 16,
        Forasteiro = 17,
        Gladiador = 18,
        Guarda = 19,
        Herdeiro = 20,
        HeroiCampones = 21,
        Marujo = 22,
        Mateiro = 23,
        MembroDeGuilda = 24,
        Mercador = 25,
        Minerador = 26,
        Nomade = 27,
        Pivete = 28,
        Refugiado = 29,
        Seguidor = 30,
        Selvagem = 31,
        Taverneiro = 32,
        Trabalhador = 33
    }

    /// <summary>
    /// Os 20 Deuses Maiores do Panteão de Arton (Tormenta20 Jogo do Ano) + Não Devoto.
    /// </summary>
    public enum T20Deity
    {
        NaoDevoto = 0,
        Valkaria = 1,
        Aharadak = 2,
        Allihanna = 3,
        Arsenal = 4,
        Azgher = 5,
        Hyninn = 6,
        Kallyadranoch = 7,
        Khalmyr = 8,
        Lena = 9,
        LinWu = 10,
        Marah = 11,
        Megalokk = 12,
        Nimb = 13,
        Oceano = 14,
        Sszzaas = 15,
        TannaToh = 16,
        Tenebra = 17,
        Thwor = 18,
        Thyatis = 19,
        Wynna = 20
    }
}
