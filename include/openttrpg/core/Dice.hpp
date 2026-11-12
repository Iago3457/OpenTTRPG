#pragma once

#include "openttrpg/data_structures/LinkedList.hpp"
#include <cstdint>
#include <random>
#include <string>

namespace openttrpg::core {

/**
 * @brief Define quais dados de um grupo entram na soma.
 *
 * Genérico o suficiente para cobrir:
 * - Vantagem/Desvantagem (2d20, mantém o maior/menor)
 * - Geração de atributos "4d6, descarta o menor"
 * - Pools de dados de sistemas narrativos
 */
enum class KeepMode : int {
    All = 0,     // Todos os dados somam
    Highest = 1, // Mantém os N maiores
    Lowest = 2   // Mantém os N menores
};

/**
 * @brief Um único dado rolado, com metadados para exibição na interface.
 */
struct DieRoll {
    int sides = 0;         // Faces do dado (ex: 20)
    int value = 0;         // Valor obtido
    bool kept = true;      // Se entrou na soma (false = descartado por keep/drop)
    bool exploded = false; // Se é um dado extra gerado por explosão
    int sign = 1;          // +1 ou -1 (termo subtraído, ex: "1d20-1d4")
};

/**
 * @brief Um termo de dados de uma expressão (ex: "4d6kh3", "2d20kl1", "3d6!").
 */
struct DiceTerm {
    int count = 1;                 // Quantidade de dados
    int sides = 20;                // Faces de cada dado
    KeepMode keep = KeepMode::All; // Política de manutenção
    int keepCount = 0;             // Quantos dados manter (se keep != All)
    bool exploding = false;        // Rola novamente ao tirar o valor máximo
    int sign = 1;                  // +1 ou -1
};

/**
 * @brief Resultado completo de uma rolagem, independente de sistema.
 *
 * O Core NÃO interpreta críticos: ele só informa quantos dados mantidos
 * tiraram o valor máximo/mínimo. Cada sistema (T20, D&D, etc.) decide o que isso significa.
 */
struct DiceRollResult {
    int total = 0;                 // diceTotal + modifier
    int diceTotal = 0;             // Soma (com sinal) apenas dos dados mantidos
    int modifier = 0;              // Soma dos modificadores fixos
    ds::LinkedList<DieRoll> rolls; // Todos os dados rolados, na ordem

    [[nodiscard]] int countNaturalMax() const; // Dados mantidos com valor == faces
    [[nodiscard]] int countNaturalMin() const; // Dados mantidos com valor == 1
    [[nodiscard]] std::string format() const;  // Ex: "[~3, 5, 6] + 2 = 13"
};

/**
 * @brief Motor de dados genérico e determinístico (com semente) do Core.
 *
 * Gramática aceita por rollExpression (sem diferenciar maiúsculas e ignorando espaços):
 *   expr  := termo (('+' | '-') termo)*
 *   termo := numero | [N]d(X|%)[!][(k|kh|kl|d|dh|dl)[M]]
 *
 * Exemplos: "1d20+5", "2d20kh1+3" (vantagem), "4d6dl1" (descarta menor),
 *           "3d6!" (explosivo), "1d8+1d6-2", "d%".
 */
class DiceEngine {
public:
    static constexpr int kMaxDicePerTerm = 1000;
    static constexpr int kMaxSides = 100000;
    static constexpr int kMaxExplosionsPerDie = 100;

    DiceEngine();                             // Semente aleatória
    explicit DiceEngine(std::uint32_t seed);  // Semente fixa (testes/replays/multiplayer)

    void reseed(std::uint32_t seed);

    // Rola um único dado de X faces (1..X). Lança std::invalid_argument se X for inválido.
    int rollDie(int sides);

    // NdX + modificador
    DiceRollResult roll(int count, int sides, int modifier = 0);

    // NdX mantendo os N maiores/menores
    DiceRollResult rollKeep(int count, int sides, KeepMode mode, int keepCount, int modifier = 0);

    // Atalhos de vantagem/desvantagem (2dX mantém 1)
    DiceRollResult rollAdvantage(int sides = 20, int modifier = 0);
    DiceRollResult rollDisadvantage(int sides = 20, int modifier = 0);

    // NdX explosivo (rola de novo ao tirar o máximo)
    DiceRollResult rollExploding(int count, int sides, int modifier = 0);

    // Rola um conjunto arbitrário de termos
    DiceRollResult rollTerms(const ds::LinkedList<DiceTerm>& terms, int modifier = 0);

    // Interpreta e rola uma expressão. Lança std::invalid_argument se a expressão for inválida.
    DiceRollResult rollExpression(const std::string& expression);

    // Apenas interpreta a expressão (útil para validar entrada da UI sem rolar)
    static bool parseExpression(const std::string& expression,
                                ds::LinkedList<DiceTerm>& outTerms,
                                int& outModifier,
                                std::string* outError = nullptr);

private:
    std::mt19937 rng_;

    void rollTermInto(const DiceTerm& term, DiceRollResult& result);
};

} // namespace openttrpg::core
