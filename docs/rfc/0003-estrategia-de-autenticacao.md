# RFC 0003 — Estratégia de autenticação

- **Status**: Aceito
- **Autor**: Taynan Konrath
- **Data**: 2026-08-30

## Contexto

O desafio exige proteger rotas sensíveis com "autenticação via CPF", através de uma Function
Serverless que valida o CPF, consulta a existência/status do cliente no banco e devolve um JWT.
A aplicação já tinha, desde fases anteriores, um login administrativo (e-mail/senha) para a
equipe da oficina (Administrador/Mecânico/Recepcionista). A pergunta era como encaixar os dois
perfis (cliente final x staff) num único esquema de autorização, sem duplicar infraestrutura de
autenticação.

## Alternativas consideradas

| Opção | Prós | Contras |
|---|---|---|
| **JWT único, dois emissores, mesma chave/issuer/audience (escolhida)** | A API principal só precisa validar assinatura/expiração (já fazia isso); nenhuma dependência de rede entre a API e a function no caminho de autorização; simples de simular em testes de integração (basta assinar um token com a mesma chave) | Emissor e API precisam compartilhar o segredo HS256 fora de banda (via secrets do GitHub, não versionado) |
| Sessão compartilhada (Redis/DB) validada a cada request | Revogação de token imediata | Acopla a API a um serviço de sessão adicional; nenhum requisito do desafio pede revogação imediata, e adiciona um ponto de falha extra pra um MVP acadêmico |
| API valida o CPF diretamente (sem function separada) | Menos peças móveis | Contraria o requisito explícito do desafio de ter uma Function Serverless dedicada para autenticação via CPF |
| OAuth2/OIDC completo (Cognito, Auth0) | Padrão de mercado, revogação/refresh tokens prontos | Overhead de configuração desproporcional ao escopo (curso, não produto real); nenhum requisito pede OIDC |

## Decisão

Dois emissores de JWT, ambos assinados com o **mesmo segredo simétrico (HS256), issuer e
audience** — qualquer token de qualquer um dos dois é aceito pela API principal sem nenhuma
chamada de rede adicional entre os serviços:

1. **`POST /api/autenticacao/login`** (API principal, login e-mail/senha) → token com
   `role = Administrador | Mecanico | Recepcionista`.
2. **`POST /auth/login`** (Lambda [oficina-mecanica-auth](https://github.com/konrath9/oficina-mecanica-auth),
   login por CPF) → valida o CPF (dígito verificador), consulta `clientes` no RDS
   (existência + `ativo`), e emite um token com `role = Cliente` e `sub = ClienteId`.

Autorização por role nos controllers:

- Endpoints administrativos (`/api/clientes`, `/api/ordens-servico`, `/api/veiculos`,
  `/api/servicos`, `/api/pecas`): exigem `Administrador`, `Mecanico` ou `Recepcionista` — um
  token `Cliente` recebe 403.
- `/api/acompanhamento/*` (consulta de status, aprovação/recusa de orçamento): aceita staff
  (qualquer OS) ou `Cliente` restrito à própria OS (`sub` do token == `ClienteId` da OS).

Ver o fluxo completo no [diagrama de sequência](../diagramas/sequencia-autenticacao-e-abertura-os.md).

## Consequências

- **Positivo**: a API principal nunca precisa saber que a function existe — ela só valida um JWT
  padrão. Isso também simplifica testes de integração (`GerarTokenCliente` no
  `OficinaMecanicaWebApplicationFactory` assina um token idêntico ao da Lambda, sem precisar
  subir a function de verdade).
- **Negativo/risco operacional**: como o segredo é compartilhado por convenção (mesmo valor
  hardcoded como *default* em ambos os repositórios para demo local), em produção real é
  necessário garantir manualmente que o secret `JWT_SECRET_KEY` do repositório
  `oficina-mecanica-auth` e o secret `PROD_JWT_SECRET_KEY` do repositório principal tenham o
  **mesmo valor** — não há verificação automática disso; é um ponto de atenção documentado no
  README do repositório principal.
- **Negativo**: sem revogação de token antes da expiração (1h para clientes, 24h para staff) —
  desativar um cliente (`PATCH /api/clientes/{id}/status`) impede *novos* logins, mas um token já
  emitido continua válido até expirar.
