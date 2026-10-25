CXX = g++
CXXFLAGS = -std=c++17 -Wall -Wextra -O2 -fPIC -MMD -MP -Iinclude

SRC_DIR = src
INC_DIR = include
BUILD_DIR = build
BIN_DIR = bin
TEST_DIR = tests
CSHARP_PROJ = bindings/csharp/OpenTTRPG.CLI.csproj

# Fontes da aplicação (excluindo main para poder compilar testes e DLL separadamente)
CORE_SRCS = $(filter-out $(SRC_DIR)/main.cpp, $(shell find $(SRC_DIR) -name '*.cpp' 2>/dev/null))
CORE_OBJS = $(patsubst $(SRC_DIR)/%.cpp, $(BUILD_DIR)/%.o, $(CORE_SRCS))

MAIN_SRC = $(SRC_DIR)/main.cpp
MAIN_OBJ = $(BUILD_DIR)/main.o

DEPS = $(CORE_OBJS:.o=.d) $(MAIN_OBJ:.o=.d)

CLI_TARGET = $(BIN_DIR)/openttrpg_cli
TEST_TARGET = $(BIN_DIR)/test_data_structures
TEST_GENERIC_TARGET = $(BIN_DIR)/test_generic_core
SHARED_TARGET = $(BIN_DIR)/libopenttrpg_core.so

.PHONY: all clean run run-cpp run-csharp test test_generic lib dirs

all: dirs $(CLI_TARGET) $(SHARED_TARGET)

dirs:
	@mkdir -p $(BUILD_DIR) $(BIN_DIR)

$(BUILD_DIR)/%.o: $(SRC_DIR)/%.cpp
	@mkdir -p $(dir $@)
	$(CXX) $(CXXFLAGS) -c $< -o $@

$(CLI_TARGET): $(CORE_OBJS) $(MAIN_OBJ)
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(CORE_OBJS) $(MAIN_OBJ) -o $@
	@echo "==> Executável CLI atualizado: $(CLI_TARGET)"

lib: $(SHARED_TARGET)

$(SHARED_TARGET): $(CORE_OBJS)
	@mkdir -p $(BIN_DIR)
	$(CXX) -shared $(CXXFLAGS) $(CORE_OBJS) -o $@
	@echo "==> Biblioteca compartilhada (DLL/.so) atualizada: $(SHARED_TARGET)"

test: dirs $(TEST_TARGET) $(TEST_GENERIC_TARGET)
	@echo "==> Executando bateria de testes AED1 (Tormenta20)..."
	@./$(TEST_TARGET)
	@echo "\n==> Executando bateria de testes do Core Genérico e C API (DLL)..."
	@./$(TEST_GENERIC_TARGET)

test_generic: dirs $(TEST_GENERIC_TARGET)
	@./$(TEST_GENERIC_TARGET)

$(TEST_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_data_structures.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_data_structures.cpp $(CORE_OBJS) -o $@

$(TEST_GENERIC_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_generic_core.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_generic_core.cpp $(CORE_OBJS) -o $@

run: all
	@if command -v dotnet >/dev/null 2>&1; then \
		echo "==> Executando CLI mais atualizado via C# (.NET + $(SHARED_TARGET))..."; \
		[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
		LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ); \
	else \
		echo "==> Executando CLI mais atualizado ($(CLI_TARGET))..."; \
		./$(CLI_TARGET); \
	fi

run-cpp: all
	@./$(CLI_TARGET)

run-csharp: $(SHARED_TARGET)
	@[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ)

clean:
	@rm -rf $(BUILD_DIR) $(BIN_DIR)
	@echo "==> Diretórios de build limpos."

-include $(DEPS)
