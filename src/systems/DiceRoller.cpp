#include "openttrpg/systems/DiceRoller.hpp"
#include <sstream>
#include <regex>
#include <chrono>

namespace openttrpg::systems {

std::string DiceResult::format() const {
    std::ostringstream oss;
    oss << count << "d" << sides;
    if (modifier > 0) {
        oss << "+" << modifier;
    } else if (modifier < 0) {
        oss << modifier;
    }
    oss << " -> [";
    for (std::size_t i = 0; i < individualRolls.size(); ++i) {
        oss << individualRolls[i];
        if (i + 1 < individualRolls.size()) {
            oss << ", ";
        }
    }
    oss << "] ";
    if (modifier != 0) {
        oss << "(Soma: " << diceSum << (modifier > 0 ? " + " : " - ")
            << std::abs(modifier) << ") ";
    }
    oss << "= " << total;

    // Notação especial para regras de Tormenta20
    if (isNatural20) {
        oss << " *** [20 NATURAL! ACERTO CRÍTICO NO T20!] ***";
    } else if (isNatural1) {
        oss << " *** [1 NATURAL! FALHA CRÍTICA NO T20!] ***";
    }

    return oss.str();
}

DiceRoller::DiceRoller() {
    auto seed = static_cast<unsigned int>(std::chrono::system_clock::now().time_since_epoch().count());
    rng.seed(seed);
}

DiceRoller::DiceRoller(unsigned int seed) : rng(seed) {}

DiceResult DiceRoller::roll(int count, int sides, int modifier) {
    if (count <= 0) count = 1;
    if (sides <= 0) sides = 6;

    std::uniform_int_distribution<int> dist(1, sides);
    DiceResult result;
    result.count = count;
    result.sides = sides;
    result.modifier = modifier;
    result.diceSum = 0;
    result.isNatural20 = false;
    result.isNatural1 = false;

    for (int i = 0; i < count; ++i) {
        int val = dist(rng);
        result.individualRolls.push_back(val);
        result.diceSum += val;

        // Regra de T20 para d20
        if (sides == 20) {
            if (val == 20) result.isNatural20 = true;
            if (val == 1)  result.isNatural1 = true;
        }
    }

    result.total = result.diceSum + modifier;
    return result;
}

DiceResult DiceRoller::rollTest(int modifier) {
    return roll(1, 20, modifier);
}

DiceResult DiceRoller::rollD20(int modifier) {
    return rollTest(modifier);
}

DiceResult DiceRoller::rollDamage(int diceCount, int diceSides, int criticalMultiplier, int bonus) {
    // No Tormenta20, o multiplicador de crítico multiplica a QUANTIDADE de dados de dano
    int totalCount = diceCount * std::max(1, criticalMultiplier);
    return roll(totalCount, diceSides, bonus);
}

DiceResult DiceRoller::rollExpression(const std::string& expr) {
    // Regex para capturar: [count]d[sides]([+-][mod])?
    // Ex: "1d20+5", "2d6-2", "d20", "3d8"
    std::regex rgx(R"((\d*)d(\d+)(?:([+-])(\d+))?)", std::regex::icase);
    std::smatch match;

    if (std::regex_search(expr, match, rgx)) {
        int count = match[1].str().empty() ? 1 : std::stoi(match[1].str());
        int sides = std::stoi(match[2].str());
        int mod = 0;

        if (match[3].matched && match[4].matched) {
            int modVal = std::stoi(match[4].str());
            mod = (match[3].str() == "-") ? -modVal : modVal;
        }

        return roll(count, sides, mod);
    }

    // Fallback: d20 simples
    return rollTest(0);
}

} // namespace openttrpg::systems
