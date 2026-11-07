# Roteiro do vídeo de demonstração — Fase 3

Duração alvo: **15 min** (limite do desafio). Os tempos são acumulados.

Requisitos que o vídeo precisa cobrir (conforme o enunciado):
autenticação com CPF · execução da pipeline CI/CD · deploy automatizado ·
consumo das APIs protegidas · dashboard de monitoramento com análise ao vivo ·
logs e traces em execução.

---

## Parte 0 — Preparação (30-40 min ANTES de gravar)

> A pipeline leva ~8 min. Não dá pra esperar ela inteira gravando — a estratégia
> é disparar uma no início e voltar nela no meio do vídeo, já concluída.

**0.1. Subir o ambiente**
1. AWS Academy → **Start Lab** → esperar o círculo verde
2. **AWS Details → AWS CLI → Show** → copiar e colar em `C:\Users\tayna\.aws\credentials`
3. ```bash
   bash scripts/retomar-ambiente.sh
   ```
   Ele propaga as credenciais, liga a EC2, valida o RDS e dispara um redeploy.
   Se avisar que o **IP mudou**, atualize os secrets que ele indica e troque o IP
   em todos os comandos abaixo.

**0.2. Esperar a pipeline terminar** (~8 min)
```bash
gh run watch -R konrath9/OficinaMecanica
```

**0.3. Popular dados** (senão os painéis de negócio ficam vazios)
```bash
bash scripts/popular-dados-demo.sh 98.82.219.28 8
```

**0.4. Criar OS "em andamento"** para aparecerem na listagem administrativa
(a listagem esconde OS `Finalizada`/`Entregue`). Rode o script de novo e
**interrompa com Ctrl+C** depois de ver a 2ª linha `[2] OS-... -> entregue`.

**0.5. Deixar aberto em abas separadas do navegador**
| Aba | URL |
|---|---|
| Swagger | `http://98.82.219.28/swagger` |
| Dashboard New Relic | https://one.newrelic.com/redirect/entity/ODQ5ODAyMHxWSVp8REFTSEJPQVJEfGRhOjEzMTYyMjkw |
| Logs New Relic | one.newrelic.com → **Logs** |
| Traces New Relic | one.newrelic.com → **APM & Services** → `oficina-mecanica-api` |
| GitHub — 4 repos | um em cada aba |
| GitHub Actions | `github.com/konrath9/OficinaMecanica/actions` |
| Branch protection | `github.com/konrath9/OficinaMecanica/settings/branches` |

**0.6. Dois terminais abertos**, fonte grande (Ctrl+Shift+`+`):
- **T1** — comandos `curl`
- **T2** — logs do cluster:
  ```bash
  ssh -i "/d/ProjetosVSS/Pós/oficina-mecanica-infra-k8s/k3s_ec2_key" ubuntu@98.82.219.28
  sudo k3s kubectl logs -f deployment/oficina-mecanica-api -n oficina-mecanica
  ```

**0.7. Logo antes de apertar REC**: faça uma alteração boba (ex.: uma linha no
README), commit numa branch, abra o PR. Você vai mergear isso no ar, e é o
merge que dispara a pipeline que você mostra depois.

---

## Parte 1 — Abertura e arquitetura · 0:00 → 1:30

**Mostrar**: `docs/diagramas/componentes.md` renderizado no GitHub.

**Falar**:
> "Fase 3 do Tech Challenge. A aplicação da oficina foi segregada em 4
> repositórios, cada um com CI/CD e Terraform próprios, rodando na AWS.
>
> O cliente entra pelo **API Gateway**. A rota de login vai para uma **Function
> Serverless** — um Lambda .NET 8 que valida o CPF, consulta o cliente no **RDS
> PostgreSQL** e devolve um JWT. As demais rotas são roteadas para o **cluster
> Kubernetes**, onde o Traefik faz o ingress até a aplicação principal, que
> escala de 1 a 3 réplicas por HPA.
>
> Toda a telemetria — traces, métricas e logs — sai da aplicação por
> **OpenTelemetry** para o **New Relic**."

Abrir `docs/` rapidamente: "a documentação arquitetural está versionada junto —
3 RFCs, 4 ADRs, diagramas de componentes e sequência, e o modelo de dados com
o diagrama ER e a justificativa dos relacionamentos."

---

## Parte 2 — Repositórios, CI/CD e proteção de branch · 1:30 → 3:00

**Mostrar**: os 4 repos, depois `Settings → Branches`.

**Falar**:
> "Os quatro repositórios: a aplicação principal, o Lambda de autenticação, a
> infraestrutura do Kubernetes e a do banco gerenciado — as duas últimas em
> Terraform."

**Mostrar** a tela de branch protection:
> "A branch principal é protegida: sem commit direto, merge só por Pull Request,
> sem force-push e sem deleção. Isso vale nos quatro repositórios."

**Mostrar** a aba de PRs fechados: "todo o trabalho entrou por PR."

---

## Parte 3 — Pipeline CI/CD ao vivo · 3:00 → 5:30

**Mostrar**: o PR que você preparou no passo 0.7 → clicar em **Merge**.

**Falar**:
> "Vou mergear este PR e a pipeline dispara sozinha."

Ir para a aba **Actions**, abrir o run que acabou de começar.

**Falar, enquanto abre o `ci-cd.yml`**:
> "A pipeline tem dois estágios. O primeiro é **homologação**: restore, build,
> `dotnet test` com a suíte completa, build e push da imagem para o GHCR, e aí
> o Terraform sobe um cluster `kind` efêmero dentro do próprio runner, aplica
> os manifestos, espera o rollout e roda um smoke test. No fim, destrói o
> cluster. É um ambiente descartável que valida o deploy inteiro antes de
> qualquer coisa chegar em produção.
>
> O segundo estágio só roda se o primeiro passar inteiro: é o **deploy de
> produção** no cluster k3s."

**Mostrar** o run já concluído do passo 0.2 (verde), abrindo o job
`Deploy no cluster k3s de producao`:
> "Aqui, o run de alguns minutos atrás. Repare no passo que gera o Secret de
> produção: a connection string do RDS, a chave JWT e a license key do New Relic
> vêm dos secrets do GitHub — o `secret.yaml` versionado no repositório só tem
> credencial de demonstração, nunca a real. Os manifestos vão por SSH, porque a
> porta 6443 do Kubernetes não é exposta à internet."

Abrir o passo **Smoke test via API Gateway**: "e ele termina validando a
aplicação já publicada, passando pelo Traefik."

---

## Parte 4 — Deploy automatizado e o cluster · 5:30 → 7:00

**T2** (a sessão SSH), pare o `logs -f` com Ctrl+C:
```bash
sudo k3s kubectl get pods,svc,ingress,hpa -n oficina-mecanica
```

**Falar**:
> "Este é o cluster de produção. O deploy que a pipeline acabou de fazer:
> o Deployment da API, o Service, o Ingress do Traefik — que é o API Gateway na
> entrada do cluster — e o HPA, configurado para escalar de 1 a 3 réplicas a
> 70% de CPU."

```bash
sudo k3s kubectl describe hpa oficina-mecanica-api -n oficina-mecanica | head -20
```
> "As métricas vêm do metrics-server; o HPA está lendo o consumo real dos pods."

---

## Parte 5 — Autenticação com CPF · 7:00 → 9:00

**T1**:
```bash
curl -i -X POST https://s5zehub8y3.execute-api.us-east-1.amazonaws.com/auth/login \
  -H 'Content-Type: application/json' -d '{"cpf":"11111111111"}'
```
> "CPF inválido — o Lambda valida o dígito verificador antes de tocar no banco.
> 401."

```bash
curl -i -X POST https://s5zehub8y3.execute-api.us-east-1.amazonaws.com/auth/login \
  -H 'Content-Type: application/json' -d '{"cpf":"52998224725"}'
```
> "CPF válido, mas que não existe na base. Também 401 — a function consulta a
> existência e o status do cliente."

```bash
TOKEN=$(curl -s -X POST https://s5zehub8y3.execute-api.us-east-1.amazonaws.com/auth/login \
  -H 'Content-Type: application/json' -d '{"cpf":"74185296355"}' \
  | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
echo $TOKEN
```
> "Agora o CPF do João, que existe e está ativo: veio o JWT."

Cole o token em **jwt.io** e mostre o payload:
> "Repare: `sub` é o id do cliente, e a role é `Cliente`. É esse token que
> as APIs protegidas vão validar."

**Diferencial pra citar**:
> "A aplicação principal não chama a function pra validar o token — ela só
> confere a assinatura, porque os dois compartilham a mesma chave, issuer e
> audience. Sem chamada de rede no caminho de autorização."

---

## Parte 6 — Consumo das APIs protegidas · 9:00 → 11:00

```bash
curl -i http://98.82.219.28/api/ordens-servico
```
> "Rota sensível sem token: 401."

```bash
curl -i -H "Authorization: Bearer $TOKEN" http://98.82.219.28/api/clientes
```
> "Agora com o token do cliente, numa rota administrativa: **403**. O cliente
> autenticou, mas não tem permissão — a gestão de clientes é restrita aos
> perfis de staff."

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  http://98.82.219.28/api/acompanhamento/<NUMERO-DA-OS-DO-JOAO>
```
> "Na rota de acompanhamento, o mesmo token funciona: ele vê o status da OS, o
> orçamento e o veículo."

Agora mostre o isolamento entre clientes — pegue o token da Maria e tente a OS do João:
```bash
TOKEN_MARIA=$(curl -s -X POST https://s5zehub8y3.execute-api.us-east-1.amazonaws.com/auth/login \
  -H 'Content-Type: application/json' -d '{"cpf":"85296374100"}' \
  | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
curl -i -H "Authorization: Bearer $TOKEN_MARIA" \
  http://98.82.219.28/api/acompanhamento/<NUMERO-DA-OS-DO-JOAO>
```
> "403. A Maria está autenticada, mas a OS não é dela — a API compara o `sub` do
> token com o dono da ordem."

Fecha no Swagger (`http://98.82.219.28/swagger`), abrindo um endpoint:
> "A documentação OpenAPI está publicada junto com a aplicação."

---

## Parte 7 — Dashboard de monitoramento · 11:00 → 13:00

**Mostrar**: o dashboard do New Relic.

**Falar, painel por painel**:
> "Este dashboard é provisionado por Terraform, no diretório
> `observability/newrelic` — não foi montado na mão.
>
> **Volume diário de ordens de serviço** — cada OS criada incrementa um contador
> na aplicação.
>
> **Tempo médio de execução por status** — Diagnóstico, Execução e Finalização.
> É um histograma que a aplicação alimenta a cada transição, calculando a
> duração a partir do histórico de status da própria OS.
>
> **Erros e falhas nas integrações** — falhas no envio de e-mail de notificação
> ao cliente, por exemplo.
>
> **Latência das APIs** — percentis 50 e 95 por rota, vindos da instrumentação
> OpenTelemetry."

**Análise ao vivo** — deixe uma OS rodando enquanto fala, pra o número subir:
```bash
bash scripts/popular-dados-demo.sh 98.82.219.28 2
```
Volte ao dashboard e dê refresh: "o volume acabou de subir."

**Mostrar** também (menu **Alerts**):
> "Dois alertas configurados, também por Terraform: um para falhas no
> processamento de ordens de serviço — erros de integração e HTTP 5xx nas rotas
> de OS — e outro de uptime, ligado a um monitor de Synthetics que pinga o
> `/health` a cada 5 minutos. Ambos notificam por e-mail."

Abra o monitor de Synthetics mostrando o histórico de checks verdes.

---

## Parte 8 — Logs e traces em execução · 13:00 → 14:30

**T2**, volte para o log em tempo real:
```bash
sudo k3s kubectl logs -f deployment/oficina-mecanica-api -n oficina-mecanica | tail -5
```

**T1**, dispare uma requisição com um correlation id escolhido por você:
```bash
curl -s -H "X-Correlation-Id: demo-video-001" \
     -H "Authorization: Bearer $TOKEN" \
     http://98.82.219.28/api/acompanhamento/<NUMERO-DA-OS> -i | head -12
```

**Falar**:
> "Repare que a resposta devolve o mesmo `X-Correlation-Id`."

Aponte para o T2:
> "E no log, que é JSON estruturado, esse id aparece carimbado em todas as
> linhas daquela requisição. Se o cliente reclamar de um erro, eu pego o id da
> resposta e encontro tudo que aconteceu naquela chamada."

**New Relic → APM & Services → `oficina-mecanica-api` → Distributed tracing**:
> "E aqui os traces: cada requisição HTTP instrumentada pelo OpenTelemetry, com
> a duração de ponta a ponta."

Abra um trace individual e mostre os spans.

---

## Parte 9 — Fechamento · 14:30 → 15:00

> "Recapitulando: quatro repositórios com CI/CD e branch protegida, API Gateway
> com autenticação por CPF via Lambda, banco gerenciado e cluster Kubernetes com
> escalabilidade — tudo em Terraform — e observabilidade completa com traces,
> métricas de negócio, logs correlacionados, dashboards e alertas.
>
> Os links dos repositórios e da documentação estão no PDF da entrega.
> Obrigado!"

---

## Colinha de valores

| | |
|---|---|
| App / Swagger | `http://98.82.219.28/swagger` |
| API Gateway (login) | `https://s5zehub8y3.execute-api.us-east-1.amazonaws.com/auth/login` |
| CPFs válidos (seed) | João `74185296355` · Maria `85296374100` · Carlos `96374185200` |
| CPF válido inexistente | `52998224725` |
| CPF inválido | `11111111111` |
| Staff | `demo@oficina.com` / `Demo@123456` |

## Dicas de gravação

- Grave em blocos separados e junte depois — erra menos e não precisa refazer tudo.
- Aumente a fonte do terminal antes de começar; `curl -i` mostra o status na tela.
- Se algo falhar ao vivo, não tente consertar gravando: corte e refaça o bloco.
- Ao terminar, confirme a duração **abaixo de 15:00** antes de subir no YouTube
  (público ou não listado).
