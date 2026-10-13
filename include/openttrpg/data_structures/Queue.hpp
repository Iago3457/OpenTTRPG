#pragma once

#include "Node.hpp"
#include <cstddef>
#include <stdexcept>

namespace openttrpg::ds {

/**
 * @brief Fila encadeada genérica (FIFO) para controle de iniciativa e turnos - AED1
 * @tparam T Tipo armazenado na fila.
 */
template <typename T>
class Queue {
private:
    Node<T>* frontNode;
    Node<T>* rearNode;
    std::size_t count;

public:
    Queue() : frontNode(nullptr), rearNode(nullptr), count(0) {}

    ~Queue() {
        clear();
    }

    Queue(const Queue& other) : frontNode(nullptr), rearNode(nullptr), count(0) {
        Node<T>* curr = other.frontNode;
        while (curr != nullptr) {
            enqueue(curr->data);
            curr = curr->next;
        }
    }

    Queue& operator=(const Queue& other) {
        if (this != &other) {
            clear();
            Node<T>* curr = other.frontNode;
            while (curr != nullptr) {
                enqueue(curr->data);
                curr = curr->next;
            }
        }
        return *this;
    }

    Queue(Queue&& other) noexcept : frontNode(other.frontNode), rearNode(other.rearNode), count(other.count) {
        other.frontNode = nullptr;
        other.rearNode = nullptr;
        other.count = 0;
    }

    Queue& operator=(Queue&& other) noexcept {
        if (this != &other) {
            clear();
            frontNode = other.frontNode;
            rearNode = other.rearNode;
            count = other.count;
            other.frontNode = nullptr;
            other.rearNode = nullptr;
            other.count = 0;
        }
        return *this;
    }

    void enqueue(const T& value) {
        Node<T>* newNode = new Node<T>(value);
        if (rearNode == nullptr) {
            frontNode = rearNode = newNode;
        } else {
            rearNode->next = newNode;
            rearNode = newNode;
        }
        count++;
    }

    T dequeue() {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de dequeue em fila vazia.");
        }

        Node<T>* temp = frontNode;
        T val = temp->data;
        frontNode = frontNode->next;

        if (frontNode == nullptr) {
            rearNode = nullptr;
        }

        delete temp;
        count--;
        return val;
    }

    T& front() {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de acessar front em fila vazia.");
        }
        return frontNode->data;
    }

    const T& front() const {
        if (is_empty()) {
            throw std::underflow_error("Tentativa de acessar front em fila vazia.");
        }
        return frontNode->data;
    }

    void clear() {
        while (!is_empty()) {
            dequeue();
        }
    }

    [[nodiscard]] std::size_t size() const {
        return count;
    }

    [[nodiscard]] bool is_empty() const {
        return count == 0;
    }
};

} // namespace openttrpg::ds
