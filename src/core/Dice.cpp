#include "openttrpg/core/Dice.hpp"

#include <chrono>
#include <cctype>
#include <sstream>
#include <stdexcept>

namespace openttrpg::core {

namespace {

void validateTerm(const DiceTerm& term) {
    if (term.count < 1 || term.count > DiceEngine::kMaxDicePerTerm) {
        throw std::invalid_argument("Quantidade de dados deve estar entre 1 e " +
                                    std::to_string(DiceEngine::kMaxDicePerTerm) + ".");
    }
    if (term.sides < 1 || term.sides > DiceEngine::kMaxSides) {
        throw std::invalid_argument("Número de faces deve estar entre 1 e " +
                                    std::to_string(DiceEngine::kMaxSides) + ".");
    }
    if (term.sign != 1 && term.sign != -1) {
        throw std::invalid_argument("Sinal do termo deve ser +1 ou -1.");
    }
    if (term.keep != KeepMode::All && term.keepCount < 0) {
        throw std::invalid_argument("Quantidade de dados mantidos não pode ser negativa.");
    }
}

/**
 * Parser descendente simples para expressões de dados (sem regex, sem STL containers).
 */
class ExpressionParser {
public:
    explicit ExpressionParser(const std::string& text) {
        for (char c : text) {
            if (!std::isspace(static_cast<unsigned char>(c))) {
                s_ += static_cast<char>(std::tolower(static_cast<unsigned char>(c)));
            }
        }
    }

    bool parse(ds::LinkedList<DiceTerm>& terms, int& modifier, std::string& error) {
        if (s_.empty()) {
            error = "Expressão vazia.";
            return false;
        }

        long long modifierAcc = 0;
        bool first = true;

        while (!atEnd()) {
            int sign = 1;
            if (peek() == '+' || peek() == '-') {
                sign = (peek() == '-') ? -1 : 1;
                ++pos_;
            } else if (!first) {
                error = unexpected("esperado '+' ou '-'");
                return false;
            }
            first = false;

            if (atEnd()) {
                error = "Expressão termina com um operador.";
                return false;
            }

            long long count = 0;
            bool hasCount = readNumber(count, error);
            if (!error.empty()) {
                return false;
            }

            if (!atEnd() && peek() == 'd') {
                ++pos_;
                DiceTerm term;
                term.sign = sign;
                if (hasCount && count == 0) {
                    error = "Quantidade de dados não pode ser zero.";
                    return false;
                }
                term.count = hasCount ? static_cast<int>(count) : 1;

                if (!atEnd() && peek() == '%') {
                    term.sides = 100;
                    ++pos_;
                } else {
                    long long sides = 0;
                    if (!readNumber(sides, error)) {
                        if (error.empty()) {
                            error = unexpected("faltando o número de faces após 'd'");
                        }
                        return false;
                    }
                    term.sides = static_cast<int>(sides);
                }

                if (!atEnd() && peek() == '!') {
                    term.exploding = true;
                    ++pos_;
                }

                if (!atEnd() && (peek() == 'k' || peek() == 'd')) {
                    const char kind = peek();
                    ++pos_;
                    char direction = (kind == 'k') ? 'h' : 'l';
                    if (!atEnd() && (peek() == 'h' || peek() == 'l')) {
                        direction = peek();
                        ++pos_;
                    }
                    long long n = 1;
                    if (!readNumber(n, error) && !error.empty()) {
                        return false;
                    }
                    if (n > term.count) {
                        n = term.count;
                    }
                    if (kind == 'k') {
                        term.keep = (direction == 'h') ? KeepMode::Highest : KeepMode::Lowest;
                        term.keepCount = static_cast<int>(n);
                    } else {
                        // "drop lowest N" == "keep highest (count - N)"
                        term.keep = (direction == 'l') ? KeepMode::Highest : KeepMode::Lowest;
                        term.keepCount = term.count - static_cast<int>(n);
                    }
                }

                try {
                    validateTerm(term);
                } catch (const std::invalid_argument& e) {
                    error = e.what();
                    return false;
                }
                terms.push_back(term);
            } else {
                if (!hasCount) {
                    error = unexpected("caractere inválido");
                    return false;
                }
                modifierAcc += sign * count;
                if (modifierAcc > 1000000000LL || modifierAcc < -1000000000LL) {
                    error = "Modificador fora dos limites.";
                    return false;
                }
            }
        }

        modifier = static_cast<int>(modifierAcc);
        return true;
    }

private:
    std::string s_;
    std::size_t pos_ = 0;

    [[nodiscard]] bool atEnd() const { return pos_ >= s_.size(); }
    [[nodiscard]] char peek() const { return s_[pos_]; }

    [[nodiscard]] std::string unexpected(const std::string& what) const {
        std::string near = atEnd() ? "fim da expressão" : ("'" + s_.substr(pos_, 1) + "'");
        return "Expressão inválida: " + what + " (próximo de " + near + ").";
    }

    // Retorna false se não houver dígitos (error vazio) ou se estourar o limite (error preenchido)
    bool readNumber(long long& out, std::string& error) {
        const std::size_t start = pos_;
        long long value = 0;
        while (!atEnd() && std::isdigit(static_cast<unsigned char>(peek()))) {
            value = value * 10 + (peek() - '0');
            if (value > 100000000LL) {
                error = "Número muito grande na expressão.";
                return false;
            }
            ++pos_;
        }
        if (pos_ == start) {
            return false;
        }
        out = value;
        return true;
    }
};

std::uint32_t randomSeed() {
    std::random_device rd;
    const auto now = static_cast<std::uint32_t>(
        std::chrono::steady_clock::now().time_since_epoch().count());
    return rd() ^ now;
}

} // namespace

// ---------------------------------------------------------------------------
// DiceRollResult
// ---------------------------------------------------------------------------

int DiceRollResult::countNaturalMax() const {
    int n = 0;
    rolls.for_each([&](const DieRoll& d, std::size_t) {
        if (d.kept && d.value == d.sides) {
            ++n;
        }
    });
    return n;
}

int DiceRollResult::countNaturalMin() const {
    int n = 0;
    rolls.for_each([&](const DieRoll& d, std::size_t) {
        if (d.kept && d.value == 1) {
            ++n;
        }
    });
    return n;
}

std::string DiceRollResult::format() const {
    std::ostringstream oss;
    oss << "[";
    rolls.for_each([&](const DieRoll& d, std::size_t idx) {
        if (idx > 0) {
            oss << ", ";
        }
        if (d.sign < 0) oss << "-";
        if (!d.kept) oss << "~"; // descartado
        oss << d.value;
        if (d.exploded) oss << "!";
    });
    oss << "]";
    if (modifier > 0) {
        oss << " + " << modifier;
    } else if (modifier < 0) {
        oss << " - " << -modifier;
    }
    oss << " = " << total;
    return oss.str();
}

// ---------------------------------------------------------------------------
// DiceEngine
// ---------------------------------------------------------------------------

DiceEngine::DiceEngine() : rng_(randomSeed()) {}

DiceEngine::DiceEngine(std::uint32_t seed) : rng_(seed) {}

void DiceEngine::reseed(std::uint32_t seed) {
    rng_.seed(seed);
}

int DiceEngine::rollDie(int sides) {
    if (sides < 1 || sides > kMaxSides) {
        throw std::invalid_argument("Número de faces inválido: " + std::to_string(sides));
    }
    std::uniform_int_distribution<int> dist(1, sides);
    return dist(rng_);
}

void DiceEngine::rollTermInto(const DiceTerm& term, DiceRollResult& result) {
    validateTerm(term);

    ds::LinkedList<DieRoll> termRolls;
    for (int i = 0; i < term.count; ++i) {
        int value = rollDie(term.sides);
        termRolls.push_back(DieRoll{term.sides, value, true, false, term.sign});

        if (term.exploding && term.sides > 1) {
            int explosions = 0;
            while (value == term.sides && explosions < kMaxExplosionsPerDie) {
                value = rollDie(term.sides);
                termRolls.push_back(DieRoll{term.sides, value, true, true, term.sign});
                ++explosions;
            }
        }
    }

    if (term.keep != KeepMode::All) {
        const int total = static_cast<int>(termRolls.size());
        const int toKeep = term.keepCount > total ? total : term.keepCount;

        termRolls.for_each_mut([](DieRoll& d, std::size_t) { d.kept = false; });

        // Seleção repetida do maior/menor ainda não mantido (O(n*k), n pequeno)
        for (int k = 0; k < toKeep; ++k) {
            std::size_t best = ds::LinkedList<DieRoll>::npos;
            int bestValue = 0;
            termRolls.for_each([&](const DieRoll& d, std::size_t idx) {
                if (d.kept) {
                    return;
                }
                const bool better = (best == ds::LinkedList<DieRoll>::npos) ||
                                    (term.keep == KeepMode::Highest ? d.value > bestValue
                                                                    : d.value < bestValue);
                if (better) {
                    best = idx;
                    bestValue = d.value;
                }
            });
            termRolls.get(best).kept = true;
        }
    }

    termRolls.for_each([&](const DieRoll& d, std::size_t) {
        if (d.kept) {
            result.diceTotal += d.sign * d.value;
        }
        result.rolls.push_back(d);
    });
}

DiceRollResult DiceEngine::rollTerms(const ds::LinkedList<DiceTerm>& terms, int modifier) {
    DiceRollResult result;
    result.modifier = modifier;
    terms.for_each([&](const DiceTerm& term, std::size_t) { rollTermInto(term, result); });
    result.total = result.diceTotal + result.modifier;
    return result;
}

DiceRollResult DiceEngine::roll(int count, int sides, int modifier) {
    DiceTerm term;
    term.count = count;
    term.sides = sides;
    ds::LinkedList<DiceTerm> terms;
    terms.push_back(term);
    return rollTerms(terms, modifier);
}

DiceRollResult DiceEngine::rollKeep(int count, int sides, KeepMode mode, int keepCount, int modifier) {
    DiceTerm term;
    term.count = count;
    term.sides = sides;
    term.keep = mode;
    term.keepCount = keepCount;
    ds::LinkedList<DiceTerm> terms;
    terms.push_back(term);
    return rollTerms(terms, modifier);
}

DiceRollResult DiceEngine::rollAdvantage(int sides, int modifier) {
    return rollKeep(2, sides, KeepMode::Highest, 1, modifier);
}

DiceRollResult DiceEngine::rollDisadvantage(int sides, int modifier) {
    return rollKeep(2, sides, KeepMode::Lowest, 1, modifier);
}

DiceRollResult DiceEngine::rollExploding(int count, int sides, int modifier) {
    DiceTerm term;
    term.count = count;
    term.sides = sides;
    term.exploding = true;
    ds::LinkedList<DiceTerm> terms;
    terms.push_back(term);
    return rollTerms(terms, modifier);
}

bool DiceEngine::parseExpression(const std::string& expression,
                                 ds::LinkedList<DiceTerm>& outTerms,
                                 int& outModifier,
                                 std::string* outError) {
    outTerms.clear();
    outModifier = 0;

    std::string error;
    ExpressionParser parser(expression);
    if (!parser.parse(outTerms, outModifier, error)) {
        outTerms.clear();
        outModifier = 0;
        if (outError != nullptr) {
            *outError = error;
        }
        return false;
    }
    return true;
}

DiceRollResult DiceEngine::rollExpression(const std::string& expression) {
    ds::LinkedList<DiceTerm> terms;
    int modifier = 0;
    std::string error;
    if (!parseExpression(expression, terms, modifier, &error)) {
        throw std::invalid_argument(error);
    }
    return rollTerms(terms, modifier);
}

} // namespace openttrpg::core
