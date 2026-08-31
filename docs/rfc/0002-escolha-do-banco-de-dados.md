# RFC 0002 — Escolha do banco de dados

- **Status**: Aceito
- **Autor**: Taynan Konrath
- **Data**: 2026-08-30

## Contexto

O desafio exige um "Banco de Dados Gerenciado (PostgreSQL, MySQL, SQL Server, etc.)" provisionado
via Terraform em repositório próprio, com justificativa formal da escolha e ajustes no modelo
relacional (ver [modelo de dados](../modelo-dados.md) para o detalhamento do ER).

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **PostgreSQL / RDS (escolhida)** | Já era o banco usado desde a Fase 1/2 da aplicação (Npgsql + EF Core); tipos nativos robustos (`numeric` para dinheiro, `boolean`, enums como texto); RDS PostgreSQL tem tier `db.t3.micro` gratuito/barato no Learner Lab | Nenhuma relevante para o escopo atual |
| MySQL / RDS | Também gerenciado pela AWS, custo equivalente | Trocar de engine exigiria reescrever o provider do EF Core (`Pomelo.EntityFrameworkCore.MySql`) e revalidar todas as migrations sem nenhum ganho técnico — a modelagem atual (tipos, precisão decimal, enums como string) já é idiomática em Postgres |
| SQL Server / RDS | Suportado pela AWS | Licenciamento mais caro mesmo no Learner Lab; sem vantagem técnica sobre Postgres pro volume de dados da oficina |
| DynamoDB (NoSQL) | Serverless, sem servidor pra gerenciar | O modelo é fortemente relacional (Cliente 1:N Veículo, Cliente/Veículo 1:N OrdemServico, com agregados e relatórios que fazem `JOIN`/`GROUP BY` — ex. tempo médio de execução por status). Modelar isso em Dynamo exigiria desnormalização manual e perderia consistência transacional sem necessidade |

## Decisão

Manter **PostgreSQL 16**, agora como **RDS gerenciado** (antes era um `StatefulSet` dentro do
próprio cluster Kubernetes, usado só em ambiente local/CI — ver
[diagrama de componentes](../diagramas/componentes.md)). Ajustes feitos na modelagem para a
Fase 3, com a mudança de infraestrutura:

- **`db.t3.micro`, 20GB gp3, `storage_encrypted=true`, `publicly_accessible=false`**: dimensionamento
  mínimo compatível com o volume de uma oficina (a aplicação não tem carga de escrita intensa) e
  com o teto de custo do Learner Lab; encriptação em repouso e ausência de IP público são
  obrigatórios mesmo em ambiente de estudo.
- **`multi_az=false`, `backup_retention_period=1`**: sem alta disponibilidade multi-AZ nem
  retenção de backup maior — decisão consciente de custo/escopo acadêmico, não recomendação para
  produção real (documentado como trade-off aceito, não omissão).
- **Rede isolada**: RDS vive em subnets privadas dedicadas (`10.0.2.0/24`, `10.0.3.0/24`), acessível
  só a partir da VPC (porta 5432 liberada apenas para o CIDR `10.0.0.0/16`) — nem a aplicação nem
  o cluster k3s alcançam o banco pela internet pública.
- **Coluna `ativo` em `clientes`** (migration `AddClienteAtivo`): adicionada nesta fase porque a
  Function Serverless de login via CPF precisa negar autenticação para clientes desativados —
  antes da Fase 3 não havia necessidade de um campo de status no cliente.
- **Itens de OS (`ordem_servico_servicos`, `ordem_servico_pecas`) guardam uma cópia (snapshot) do
  preço e da descrição do serviço/peça no momento da OS**, em vez de só uma FK para o catálogo —
  ver justificativa completa em [modelo-dados.md](../modelo-dados.md).

## Consequências

- **Positivo**: nenhuma mudança de código de acesso a dados (mesma connection string via
  `Npgsql`, mesmas migrations do EF Core) — só a infraestrutura por trás mudou de "Postgres em
  StatefulSet" para "Postgres gerenciado (RDS)". O `k8s/secret.yaml` do repositório principal
  continua com credenciais de demonstração locais; em produção a connection string real do RDS é
  injetada via secret do GitHub Actions no job de deploy (nunca versionada).
- **Negativo**: sem multi-AZ, uma falha na instância RDS causa indisponibilidade até o restore
  automático da AWS — aceitável para o escopo do desafio, não para uma oficina real em produção.
