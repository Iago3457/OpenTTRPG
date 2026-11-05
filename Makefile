CXX = g++
CXXFLAGS = -std=c++17 -Wall -Wextra -O2 -fPIC -MMD -MP -Iinclude
ASAN_FLAGS = -std=c++17 -Wall -Wextra -O1 -g -fsanitize=address,leak -fno-omit-frame-pointer -Iinclude

SRC_DIR = src
INC_DIR = include
BUILD_DIR = build
BIN_DIR = bin
TEST_DIR = tests
CSHARP_PROJ = bindings/csharp/OpenTTRPG.CLI.csproj

# A biblioteca compartilhada (DLL / .so) compila EXCLUSIVAMENTE o Core genérico e a C API
CORE_SRCS = $(wildcard $(SRC_DIR)/core/*.cpp) $(wildcard $(SRC_DIR)/api/*.cpp)
CORE_OBJS = $(patsubst $(SRC_DIR)/%.cpp, $(BUILD_DIR)/%.o, $(CORE_SRCS))

DEPS = $(CORE_OBJS:.o=.d)

TEST_TARGET = $(BIN_DIR)/test_data_structures
TEST_GENERIC_TARGET = $(BIN_DIR)/test_generic_core
TEST_ASAN_TARGET = $(BIN_DIR)/test_asan
SHARED_TARGET = $(BIN_DIR)/libopenttrpg_core.so

.PHONY: all clean run run-csharp test test_generic test_asan test_csharp lib dirs

all: dirs $(SHARED_TARGET)

dirs:
	@mkdir -p $(BUILD_DIR) $(BIN_DIR)

$(BUILD_DIR)/%.o: $(SRC_DIR)/%.cpp
	@mkdir -p $(dir $@)
	$(CXX) $(CXXFLAGS) -c $< -o $@

lib: $(SHARED_TARGET)

$(SHARED_TARGET): $(CORE_OBJS)
	@mkdir -p $(BIN_DIR)
	$(CXX) -shared $(CXXFLAGS) $(CORE_OBJS) -o $@
	@echo "==> Biblioteca compartilhada agnóstica (DLL/.so) atualizada: $(SHARED_TARGET)"

test: dirs $(TEST_TARGET) $(TEST_GENERIC_TARGET) $(SHARED_TARGET)
	@echo "==> Executando bateria de testes AED1 (Estruturas de Dados Manuais)..."
	@./$(TEST_TARGET)
	@echo "\n==> Executando bateria de testes do Core Genérico e C API (DLL)..."
	@./$(TEST_GENERIC_TARGET)
	@if command -v dotnet >/dev/null 2>&1; then \
		echo "\n==> Executando bateria de testes automatizados em C# (Tormenta20 + P/Invoke)..."; \
		[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
		LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ) -- --test; \
	fi

test_generic: dirs $(TEST_GENERIC_TARGET)
	@./$(TEST_GENERIC_TARGET)

test_asan: dirs
	@echo "==> Compilando e auditando memória com AddressSanitizer (-fsanitize=address,leak)..."
	$(CXX) $(ASAN_FLAGS) $(TEST_DIR)/test_generic_core.cpp $(CORE_SRCS) -o $(TEST_ASAN_TARGET)
	@./$(TEST_ASAN_TARGET)
	@echo "==> Auditoria AddressSanitizer concluída: 0 memory leaks!"

test_csharp: $(SHARED_TARGET)
	@[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ) -- --test

$(TEST_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_data_structures.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_data_structures.cpp $(CORE_OBJS) -o $@

$(TEST_GENERIC_TARGET): $(CORE_OBJS) $(TEST_DIR)/test_generic_core.cpp
	@mkdir -p $(BIN_DIR)
	$(CXX) $(CXXFLAGS) $(TEST_DIR)/test_generic_core.cpp $(CORE_OBJS) -o $@

run: $(SHARED_TARGET)
	@echo "==> Executando cliente oficial via C# (.NET + $(SHARED_TARGET))..."
	@[ -w "$$XDG_RUNTIME_DIR" ] || export XDG_RUNTIME_DIR=/tmp; \
	LD_LIBRARY_PATH="$(abspath $(BIN_DIR)):$$LD_LIBRARY_PATH" dotnet run --project $(CSHARP_PROJ)

run-csharp: run

clean:
	@rm -rf $(BUILD_DIR) $(BIN_DIR)
	@echo "==> Diretórios de build limpos."

-include $(DEPS)
