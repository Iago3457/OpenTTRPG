#pragma once

namespace openttrpg::core {

/**
 * @brief Detalha como uma redução foi distribuída entre temporário e valor atual.
 */
struct ResourceChange {
    int absorbedByTemp = 0;   // Quanto foi consumido do valor temporário
    int appliedToCurrent = 0; // Quanto foi subtraído do valor atual
    int overflow = 0;         // Excedente que não pôde ser aplicado (abaixo do mínimo)
};

/**
 * @brief Recurso numérico genérico com mínimo, máximo e valor temporário.
 *
 * Serve para qualquer "barra" de qualquer sistema: Pontos de Vida, Pontos de Mana,
 * Sanidade, Estresse, Munição, Espaços de Magia, etc.
 *
 * - `minimum` permite sistemas onde a vida fica negativa (ex: morrendo até -10).
 * - `temp` é consumido antes do valor atual ao sofrer redução (PV temporário).
 *
 * Valores negativos em spend/restore/reduce lançam std::invalid_argument.
 */
class ResourcePool {
public:
    ResourcePool();
    explicit ResourcePool(int max, int minimum = 0);
    ResourcePool(int max, int current, int minimum);

    [[nodiscard]] int current() const;
    [[nodiscard]] int max() const;
    [[nodiscard]] int temp() const;
    [[nodiscard]] int minimum() const;
    [[nodiscard]] int effective() const;  // current + temp
    [[nodiscard]] int missing() const;    // max - current (quanto falta para encher)

    [[nodiscard]] bool isDepleted() const;  // current <= 0
    [[nodiscard]] bool isAtMinimum() const; // current <= minimum
    [[nodiscard]] bool isFull() const;      // current >= max

    // Altera o máximo; o valor atual é limitado ao novo máximo
    void setMax(int newMax);
    // Define o valor atual (limitado a [minimum, max])
    void setCurrent(int value);
    // Define o mínimo permitido (<= max); o atual é ajustado se ficar abaixo
    void setMinimum(int value);
    // Sobrescreve o valor temporário (>= 0)
    void setTemp(int value);
    // Soma ao valor temporário
    void addTemp(int amount);

    // Gasto voluntário (ex: PM para magia). Não usa temporário. Falha se ultrapassar o mínimo.
    [[nodiscard]] bool canSpend(int amount) const;
    bool spend(int amount);

    // Recupera até o máximo. Retorna quanto foi efetivamente recuperado.
    int restore(int amount);

    // Redução involuntária (ex: dano). Consome temporário primeiro, depois o atual até o mínimo.
    ResourceChange reduce(int amount);

    // Volta o valor atual ao máximo
    void refill();

private:
    int current_;
    int max_;
    int temp_;
    int minimum_;
};

} // namespace openttrpg::core
