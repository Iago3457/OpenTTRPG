#pragma once

#include <vector>
#include <string>
#include <random>

namespace openttrpg::systems {

/**
 * Guarda o resultado completo de uma rolagem de dados em Tormenta20 (Jogo do Ano).
 * 
 * Além dos valores individuais e soma, detecta regras canônicas de T20:
 * - 20 Natural: Sucesso Crítico automático em ataques e testes de perícia!
 * - 1 Natural: Falha Crítica automática!
 */
struct DiceResult {
    int count;                        // Quantidade de dados rolados (ex: 1 em 1d20, 2 em 2d6)
    int sides;                        // Quantidade de faces de cada dado (ex: 20, 6, 8)
    int modifier;                     // Bônus ou penalidade somada (ex: +5 no teste de Luta)
    std::vector<int> individualRolls; // Valores obtidos em cada dado individualmente
    int diceSum;                      // Soma dos dados brutos (sem o modificador)
    int total;                        // Total final: diceSum + modifier
    bool isNatural20;                 // Indica se rolou 20 natural no d20 (Crítico T20!)
    bool isNatural1;                  // Indica se rolou 1 natural no d20 (Falha Crítica T20!)

    // Retorna uma string formatada no padrão Tormenta20 destacando críticos
    [[nodiscard]] std::string format() const;
};

/**
 * Motor de rolagem de dados adaptado para as mecânicas de Tormenta20 (Jogo do Ano).
 */
class DiceRoller {
private:
    std::mt19937 rng;

public:
    DiceRoller();
    explicit DiceRoller(unsigned int seed);

    // Rolagem geral de N dados de X faces com modificador
    DiceResult roll(int count, int sides, int modifier = 0);

    // Teste padrão de Tormenta20 (1d20 + Modificador de Perícia ou Atributo)
    DiceResult rollTest(int modifier = 0);

    // Atalho clássico de d20
    DiceResult rollD20(int modifier = 0);

    // Rolagem de dano de arma considerando multiplicador de crítico de T20 (ex: 1d8 x2 = 2d8)
    DiceResult rollDamage(int diceCount, int diceSides, int criticalMultiplier = 1, int bonus = 0);

    // Lê expressões no padrão "1d20+5", "2d6+3", "1d8", "3d8-1"
    DiceResult rollExpression(const std::string& expr);
};

} // namespace openttrpg::systems
