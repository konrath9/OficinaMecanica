# 🔧 Oficina Mecânica - Sistema Integrado de Atendimento e Execução de Serviços

## Objetivo

MVP do back-end de uma oficina mecânica de médio porte, focado em **gestão de ordens de serviço, clientes, veículos, serviços e peças**, aplicando **Domain-Driven Design (DDD)** com boas práticas de qualidade de software e segurança.

## Tecnologias

| Tecnologia | Justificativa |
|---|---|
| **.NET 8** | LTS, alta performance, ecossistema maduro para APIs RESTful |
| **PostgreSQL 16** | Banco relacional robusto, open-source, excelente para dados transacionais de ordens de serviço, controle de estoque e relacionamentos entre clientes/veículos. Escolhido por ser gratuito, ter ótimo suporte a JSON e consultas complexas |
| **Entity Framework Core 8** | ORM produtivo com suporte a migrations e mapeamento rico do domínio |
| **JWT (Bearer Token)** | Autenticação stateless para APIs administrativas |
| **BCrypt** | Hash seguro de senhas |
| **xUnit + Moq** | Testes unitários e de integração |
| **Docker** | Containerização para execução local simplificada |

## Arquitetura

Monolito em camadas seguindo DDD:

```
OficinaMecanica.Domain         → Entidades, Value Objects, Enums (núcleo do domínio)
OficinaMecanica.Application    → Use Cases, DTOs, Interfaces
OficinaMecanica.Infrastructure → Repositórios (EF Core), Serviços, JWT, BCrypt
OficinaMecanica.API            → Controllers REST, Program.cs, Swagger
OficinaMecanica.Tests          → Testes unitários (domínio/use cases) e integração
```

## Funcionalidades

### Ordem de Serviço (OS)
- Criação com identificação do cliente (CPF/CNPJ), veículo (placa, marca, modelo, ano), serviços e peças
- Orçamento automático (soma de serviços + peças)
- Fluxo de status: **Recebida → Em Diagnóstico → Aguardando Aprovação → Em Execução → Finalizada → Entregue**
- Cancelamento com motivo
- Consulta pública de acompanhamento (sem autenticação)
- Monitoramento de tempo médio de execução

### CRUDs Administrativos (autenticados via JWT)
- Clientes (com validação de CPF/CNPJ)
- Veículos (com validação de placa — formato antigo e Mercosul)
- Serviços
- Peças e Insumos (com controle de estoque — entrada/saída)

### Segurança
- Autenticação JWT para endpoints administrativos
- Endpoint público de acompanhamento da OS (sem autenticação)
- Senhas armazenadas com BCrypt
- Validação de dados sensíveis (CPF/CNPJ, placa)

## Como Executar

### Pré-requisitos
- [Docker](https://www.docker.com/) e Docker Compose instalados

### Subir o ambiente completo

```bash
docker-compose up -d --build
```

Isso irá iniciar:
- **API** em `http://localhost:5000` (Swagger em `http://localhost:5000/swagger`)
- **PostgreSQL** na porta `5433`

### Executar localmente (sem Docker para a API)

1. Suba apenas o banco:
   ```bash
   docker-compose up -d postgres
   ```

2. Execute a API:
   ```bash
   cd OficinaMecanica.API
   dotnet run
   ```

3. Acesse o Swagger: `https://localhost:{porta}/swagger`

### Executar testes

```bash
dotnet test OficinaMecanica.Tests/OficinaMecanica.Tests.csproj --verbosity normal
```

## Testes e Cobertura de Código

O projeto possui testes automatizados (unitários e de integração), todos passando, organizados em:

| Tipo | Descrição |
|---|---|
| **Unitários — Domínio** | Entidades, Value Objects (CPF/CNPJ, Placa, ItemServico, ItemPeca) |
| **Unitários — Use Cases** | Todos os fluxos de negócio com mocks via Moq |
| **Integração** | Endpoints via `WebApplicationFactory` + banco InMemory |

A cobertura mínima exigida pelo desafio é de **80% nos domínios críticos**, requisito atendido pelo projeto. As Migrations geradas automaticamente pelo EF Core são excluídas da medição via `coverlet.runsettings`.

### Gerar relatório HTML de cobertura

```bash
# 1. Executar testes coletando cobertura
dotnet test OficinaMecanica.Tests/OficinaMecanica.Tests.csproj \
  --settings OficinaMecanica.Tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults/coverage

# 2. Gerar relatório HTML (requer reportgenerator instalado)
dotnet tool install --global dotnet-reportgenerator-globaltool   # apenas na primeira vez

reportgenerator \
  -reports:"TestResults/coverage/**/coverage.cobertura.xml" \
  -targetdir:"TestResults/Report" \
  -reporttypes:"Html"

# 3. Abrir o relatório
start TestResults/Report/index.html   # Windows
open TestResults/Report/index.html    # macOS/Linux
```

## Estrutura do Repositório

```
├── OficinaMecanica.API/            # Camada de apresentação (Controllers, Program.cs)
│   └── Dockerfile                  # Build da aplicação
├── OficinaMecanica.Application/    # Camada de aplicação (Use Cases, DTOs)
├── OficinaMecanica.Domain/         # Camada de domínio (Entidades, Value Objects)
├── OficinaMecanica.Infrastructure/ # Camada de infraestrutura (EF Core, JWT, BCrypt)
├── OficinaMecanica.Tests/          # Testes unitários e de integração
├── docker-compose.yml              # Orquestração do ambiente
└── README.md                       # Este arquivo
```

## Autores

| Nome |
|---|---|
| *Taynan Konrath* |
