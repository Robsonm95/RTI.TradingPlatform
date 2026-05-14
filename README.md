# 🚀 RTI.TradingPlatform

Uma plataforma de simulação de ordens de trading utilizando o protocolo **FIX 4.4** com **QuickFIX/N**.

O projeto é composto por duas aplicações principais:

- **RTI.OrderGenerator** → Interface web para criação e envio de ordens (`NewOrderSingle`)
- **RTI.OrderAccumulator** → Motor que recebe, valida e acumula exposição financeira por símbolo

---

## 🎯 Sobre o Projeto

Este sistema simula um fluxo real de trading onde:

- O **OrderGenerator** envia ordens via FIX para o **OrderAccumulator**.
- O acumulador calcula a **exposição financeira** por ativo em tempo real.
- Existe um limite de **R$ 100.000.000** (cem milhões) por símbolo.
- Ordens que ultrapassarem o limite são rejeitadas automaticamente com `ExecutionReport` de rejeição.

---

## 🏗️ Arquitetura

| Projeto                | Função                                          | Tecnologia                  |
|------------------------|-------------------------------------------------|-----------------------------|
| **RTI.Shared**         | Modelos, Enums, DTOs e constantes compartilhadas | .NET 8                      |
| **RTI.OrderGenerator** | Frontend + Cliente FIX                          | ASP.NET Core + HTML/JS      |
| **RTI.OrderAccumulator** | Servidor FIX + Lógica de risco/exposição      | ASP.NET Core + QuickFIX/N   |

---

## ✨ Funcionalidades

### OrderGenerator
- Interface web simples e intuitiva
- Formulário para envio de `NewOrderSingle`
- Validações no frontend e backend
- Visualização em tempo real das respostas (`ExecutionReport`)
- Suporte aos ativos: **PETR4**, **VALE3**, **VIIA4**
- **Dashboard de exposições** com atualização automática
- **Consulta histórica** de exposições por data
- **Drill-down de ordens**: clique em um símbolo para ver todas as ordens do dia

### OrderAccumulator
- Recebimento e processamento de ordens via FIX 4.4
- Cálculo de exposição financeira por símbolo:
  - `Exposição = Σ (Preço × Quantidade) compras - Σ (Preço × Quantidade) vendas`
- Persistência diária de exposições e ordens em SQLite via EF Core
- Controle rigoroso de limite por ativo (R$ 100 milhões)
- Rejeição automática de ordens que violarem o limite

---

## � Modelo de Exposição por Data (Trade Date)

### Por que quebrar por dia?

A plataforma **quebra a exposição por dia de negociação** (TradeDate). Isso é fundamental para:

| Aspecto | Benefício |
|---------|-----------|
| **Resetar limite** | O limite de R$ 100M se renova a cada dia - é impossível "travar" indefinidamente |
| **Padrão de mercado** | Alinha com ciclo real de negociação e conformidade regulatória |
| **Gestão de risco** | Reduz risco sistêmico; exposições não acumulam eternamente |
| **Auditoria** | Facilita rastreamento e reconciliação por período |
| **Histórico** | Permite consultar exposições de dias passados para análise |

### Implementação

- ✅ Cada ordem registra seu `TradeDate` (data da negociação)
- ✅ Exposição calculada por `símbolo + data`
- ✅ Limite (R$ 100M) é verificado **dentro do mesmo dia**
- ✅ Interface permite consultar exposições de qualquer data
- ✅ Cada dia tem seu próprio "universo" de exposição

**Exemplo:**
```
Dia 2026-05-13: PETR4 tem exposição de R$ 50M
Dia 2026-05-14: PETR4 começa do zero (limite disponível novamente)
```
### 📋 Drill-down de Ordens

A interface permite **explorar ordens individuais** por símbolo:

| Funcionalidade | Como usar |
|----------------|-----------|
| **Clique no símbolo** | Na tabela de exposições, clique em qualquer linha de símbolo |
| **Visualizar ordens** | Aparece tabela abaixo com todas as ordens do dia |
| **Detalhes completos** | ClOrdId, Lado, Quantidade, Preço, Status, Data/Hora |
| **Filtragem por data** | Respeita a data selecionada no filtro de exposições |

**API Endpoints:**
- `GET /exposures?tradeDate=YYYY-MM-DD` → Exposições por data
- `GET /orders?symbol=PETR4&tradeDate=YYYY-MM-DD` → Ordens por símbolo/data
---

## �🚀 Como Executar

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 ou superior (recomendado)

### Ordem de Inicialização (Importante!)

1. **Primeiro** inicie o **OrderAccumulator** (servidor FIX)
2. **Depois** inicie o **OrderGenerator** (cliente FIX)

### Terminal 1 - OrderAccumulator (Servidor)
cd RTI.OrderAccumulator
dotnet run

- O `OrderAccumulator` usa EF Core com SQLite.
- O banco local `orderAccumulator.db` é criado automaticamente na primeira execução.
- Se quiser reiniciar os dados, apague `RTI.OrderAccumulator/orderAccumulator.db` antes de iniciar.

### Terminal 2 - OrderGenerator (Cliente)
cd RTI.OrderGenerator
dotnet run

## Tecnologias Utilizadas

* .NET 10
* QuickFIX/N (FIX 4.4)
* ASP.NET Core
* C#
* HTML + JavaScript (frontend)

## Próximos Passos

* Persistência de exposição (SQLite / EF Core) — implementado
* Dashboard de exposição em tempo real
* Logging avançado e métricas
* Testes unitários e de integração
* Dockerização das aplicações