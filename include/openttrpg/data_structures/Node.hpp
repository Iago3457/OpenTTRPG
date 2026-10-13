#pragma once

namespace openttrpg::ds {

/**
 * @brief Nó genérico para estruturas lineares encadeadas (AED1).
 * @tparam T Tipo de dado armazenado.
 */
template <typename T>
struct Node {
    T data;
    Node<T>* next;

    explicit Node(const T& val) : data(val), next(nullptr) {}
    Node(const T& val, Node<T>* nextNode) : data(val), next(nextNode) {}
};

} // namespace openttrpg::ds
