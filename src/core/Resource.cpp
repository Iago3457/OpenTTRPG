#include "openttrpg/core/Resource.hpp"

#include <algorithm>
#include <limits>
#include <stdexcept>
#include <string>

namespace openttrpg::core {

namespace {

void requireNonNegative(int amount, const char* what) {
    if (amount < 0) {
        throw std::invalid_argument(std::string(what) + " não pode ser negativo.");
    }
}

} // namespace

ResourcePool::ResourcePool() : current_(0), max_(0), temp_(0), minimum_(0) {}

ResourcePool::ResourcePool(int max, int minimum) : ResourcePool(max, max, minimum) {}

ResourcePool::ResourcePool(int max, int current, int minimum)
    : current_(0), max_(0), temp_(0), minimum_(0) {
    requireNonNegative(max, "Valor máximo do recurso");
    if (minimum > max) {
        throw std::invalid_argument("Valor mínimo do recurso não pode ser maior que o máximo.");
    }
    max_ = max;
    minimum_ = minimum;
    current_ = std::clamp(current, minimum_, max_);
}

int ResourcePool::current() const { return current_; }
int ResourcePool::max() const { return max_; }
int ResourcePool::temp() const { return temp_; }
int ResourcePool::minimum() const { return minimum_; }
int ResourcePool::effective() const { return current_ + temp_; }
int ResourcePool::missing() const { return max_ - current_; }

bool ResourcePool::isDepleted() const { return current_ <= 0; }
bool ResourcePool::isAtMinimum() const { return current_ <= minimum_; }
bool ResourcePool::isFull() const { return current_ >= max_; }

void ResourcePool::setMax(int newMax) {
    requireNonNegative(newMax, "Valor máximo do recurso");
    max_ = newMax;
    if (minimum_ > max_) {
        minimum_ = max_;
    }
    current_ = std::clamp(current_, minimum_, max_);
}

void ResourcePool::setCurrent(int value) {
    current_ = std::clamp(value, minimum_, max_);
}

void ResourcePool::setMinimum(int value) {
    if (value > max_) {
        throw std::invalid_argument("Valor mínimo do recurso não pode ser maior que o máximo.");
    }
    minimum_ = value;
    current_ = std::max(current_, minimum_);
}

void ResourcePool::setTemp(int value) {
    requireNonNegative(value, "Valor temporário");
    temp_ = value;
}

void ResourcePool::addTemp(int amount) {
    requireNonNegative(amount, "Valor temporário adicionado");
    const long long sum = static_cast<long long>(temp_) + amount;
    temp_ = static_cast<int>(std::min<long long>(sum, std::numeric_limits<int>::max()));
}

bool ResourcePool::canSpend(int amount) const {
    return amount >= 0 &&
           static_cast<long long>(current_) - amount >= static_cast<long long>(minimum_);
}

bool ResourcePool::spend(int amount) {
    requireNonNegative(amount, "Custo");
    if (!canSpend(amount)) {
        return false;
    }
    current_ -= amount;
    return true;
}

int ResourcePool::restore(int amount) {
    requireNonNegative(amount, "Recuperação");
    if (current_ >= max_) {
        return 0;
    }
    const int before = current_;
    const long long cap = static_cast<long long>(max_) - current_;
    current_ = (static_cast<long long>(amount) >= cap)
        ? max_
        : static_cast<int>(static_cast<long long>(current_) + amount);
    return current_ - before;
}

ResourceChange ResourcePool::reduce(int amount) {
    requireNonNegative(amount, "Redução");
    ResourceChange change;

    const int fromTemp = std::min(temp_, amount);
    temp_ -= fromTemp;
    change.absorbedByTemp = fromTemp;

    const int remaining = amount - fromTemp;
    const long long room = static_cast<long long>(current_) - minimum_;
    const long long appliedLL = std::min<long long>(remaining, std::max<long long>(0, room));
    const int applied = static_cast<int>(appliedLL);
    current_ = static_cast<int>(static_cast<long long>(current_) - applied);
    change.appliedToCurrent = applied;
    change.overflow = remaining - applied;
    return change;
}

void ResourcePool::refill() {
    current_ = max_;
}

} // namespace openttrpg::core
