# Modelo de dados — justificativa e diagrama ER

Justificativa formal da escolha do banco está na [RFC 0002](rfc/0002-escolha-do-banco-de-dados.md).
Este documento detalha o **modelo relacional** e os ajustes feitos para a Fase 3.

## Diagrama ER

```mermaid
erDiagram
    CLIENTES ||--o{ VEICULOS : possui
    CLIENTES ||--o{ ORDENS_SERVICO : solicita
    VEICULOS ||--o{ ORDENS_SERVICO : "e alvo de"
    ORDENS_SERVICO ||--o{ ORDEM_SERVICO_SERVICOS : contem
    ORDENS_SERVICO ||--o{ ORDEM_SERVICO_PECAS : contem
    ORDENS_SERVICO ||--o{ ORDEM_SERVICO_HISTORICO_STATUS : audita
    SERVICOS ||..o{ ORDEM_SERVICO_SERVICOS : "snapshot de (sem FK)"
    PECAS ||..o{ ORDEM_SERVICO_PECAS : "snapshot de (sem FK)"

    CLIENTES {
        uuid id PK
        string nome
        string documento "CPF/CNPJ, so digitos, unico"
        string email
        string telefone
        boolean ativo "Fase 3: bloqueia login via CPF quando false"
        timestamp created_at
        timestamp updated_at
    }

    VEICULOS {
        uuid id PK
        string placa "formato antigo ou Mercosul"
        string marca
        string modelo
        int ano
        uuid cliente_id FK
    }

    USUARIOS {
        uuid id PK
        string nome
        string email UK
        string senha_hash "BCrypt"
        string perfil "Administrador | Mecanico | Recepcionista"
        boolean ativo
    }

    SERVICOS {
        uuid id PK
        string nome
        string descricao
        decimal preco
    }

    PECAS {
        uuid id PK
        string codigo UK
        string nome
        decimal preco_unitario
        int quantidade_estoque
    }

    ORDENS_SERVICO {
        uuid id PK
        string numero UK "ex: OS-2024-00001"
        uuid cliente_id FK
        uuid veiculo_id FK
        string status "Recebida..Entregue|Cancelada"
        string observacoes
        timestamp iniciada_em
        timestamp finalizada_em
        timestamp entregue_em
        timestamp created_at
        timestamp updated_at
    }

    ORDEM_SERVICO_SERVICOS {
        int id PK
        uuid ordem_servico_id FK
        uuid servico_id "snapshot, sem FK para SERVICOS"
        string descricao "copiada do catalogo no momento da OS"
        decimal preco_unitario "copiado do catalogo no momento da OS"
        int quantidade
        timestamp iniciado_em
        timestamp finalizado_em
    }

    ORDEM_SERVICO_PECAS {
        int id PK
        uuid ordem_servico_id FK
        uuid peca_id "snapshot, sem FK para PECAS"
        string codigo "copiado do catalogo"
        string descricao "copiada do catalogo"
        decimal preco_unitario "copiado do catalogo"
        int quantidade
    }

    ORDEM_SERVICO_HISTORICO_STATUS {
        int id PK
        uuid ordem_servico_id FK
        string status
        timestamp ocorrido_em
        string observacao
    }
```

## Relacionamentos e por que são assim

- **`clientes` 1—N `veiculos`** e **`clientes`/`veiculos` 1—N `ordens_servico`**: um cliente pode
  ter vários veículos e várias OS; uma OS sempre referencia exatamente um cliente e um veículo
  (FK obrigatória, `IsRequired()` nas configurações do EF Core).
- **`ordens_servico` 1—N `ordem_servico_servicos`/`ordem_servico_pecas`/`ordem_servico_historico_status`**:
  modelados como **Owned Types** do EF Core (agregado DDD — `OrdemServico` é a raiz de agregado).
  Isso significa que essas três tabelas só existem em função de uma OS: não têm repositório
  próprio, são carregadas/salvas sempre junto com a OS dona, garantindo que o agregado inteiro
  seja consistente numa única transação.
- **`ordem_servico_servicos`/`ordem_servico_pecas` guardam uma cópia do preço e da descrição do
  catálogo, sem FK real para `servicos`/`pecas`** (só um `Guid` solto, sem `.HasForeignKey()` no
  EF Core apontando pra essas tabelas). Essa é a decisão de modelagem mais importante desta
  camada, e existe por um motivo de negócio: **o valor de uma OS não pode mudar retroativamente**
  se o preço de um serviço/peça for reajustado depois. Sem o snapshot, um reajuste de preço no
  catálogo alteraria o orçamento de OS já fechadas/aprovadas — inaceitável para uma cobrança já
  comunicada ao cliente. O soft-reference ao id original (`servico_id`/`peca_id`) é mantido só
  para rastreabilidade (saber qual item do catálogo originou aquele item da OS), não para
  recalcular valores.
- **`usuarios` é independente** das demais tabelas (staff da oficina) — não tem FK de/para
  `clientes`, porque representa uma pessoa da equipe, não um cliente.
- **`clientes.ativo`** (adicionado nesta fase, migration `AddClienteAtivo`): único ajuste de
  schema motivado diretamente pela Fase 3 — a Function Serverless de login via CPF precisa negar
  autenticação para clientes desativados, e antes não havia esse conceito de status no cliente.

## Índices relevantes

- `ix_clientes_documento` (único) — impede duplicidade de CPF/CNPJ, e é a coluna usada pela
  Function Serverless na busca por CPF (`regexp_replace(documento, ...)`).
- `ix_ordens_servico_numero` (único), `ix_ordens_servico_cliente_id`, `ix_ordens_servico_veiculo_id`,
  `ix_ordens_servico_status`, `ix_ordens_servico_criada_em` — suportam as consultas mais comuns:
  busca por número (acompanhamento), listagem por cliente/veículo, filtro por status (listagem
  administrativa priorizada) e por data (relatório de tempo médio de execução, volume diário —
  ver requisito de dashboards no README).
