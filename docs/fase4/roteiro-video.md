# Roteiro do vídeo de demonstração — Fase 4

Duração alvo: **14 min**, com folga para o limite de 15. Os tempos são acumulados.

O enunciado exige que o vídeo mostre:
1. **Fluxo completo de uma OS passando pelos microsserviços** (Parte 4)
2. **Execução do Saga Pattern e tratamento de falhas** (Parte 5)
3. **Deploy automatizado de pelo menos um microsserviço com validação de testes** (Parte 3)
4. **Monitoramento e rastreamento dos fluxos distribuídos** (Parte 6)

---

## Parte 0 — Preparação (40 min ANTES de gravar)

**0.1. Subir o ambiente**
1. AWS Academy → **Start Lab** → esperar o círculo verde.
2. **AWS Details → AWS CLI → Show** → colar em `C:\Users\tayna\.aws\credentials`.
3. Rodar o script de retomada:
   ```bash
   bash scripts/retomar-ambiente.sh
   ```
   Ele propaga as credenciais, liga a EC2 e redeploya os 3 microsserviços. Se avisar que o **IP mudou**, rode os
   comandos que ele indica e troque `32.193.30.44` pelo IP novo em todo este roteiro.
4. Esperar os 3 pipelines (~10 min):
   ```bash
   for r in OficinaMecanica oficina-billing-service oficina-execution-service; do gh run watch -R konrath9/$r; done
   ```

**0.2. Validar e gerar dados para o New Relic**, que precisa de uns minutos de tráfego para os traces e o mapa de serviços aparecerem:
```bash
H=http://32.193.30.44
for c in feliz pagamento-recusado falha-execucao feliz feliz; do
  OS_URL=$H BILLING_URL=$H EXECUTION_URL=$H node scripts/demo-saga.mjs $c
done
```

**0.2b. Comprador de teste do Mercado Pago.** Em mercadopago.com.br/developers → sua aplicação → **Contas de teste**,
crie uma conta **Comprador** e anote o usuário e a senha. Os cartões de teste ficam na página **Cartões de teste** da
documentação. O nome do titular define o resultado: `APRO` = aprovado, `OTHE` = recusado. Faça um pagamento de ensaio
com `node scripts/demo-saga.mjs checkout` antes de gravar.

**0.3. Preparar o PR que você vai mergear ao vivo (Parte 3)** no **Billing Service**:
```bash
cd /d/ProjetosVSS/Pós/oficina-billing-service
git checkout main && git pull && git checkout -b demo/video-fase4
echo "" >> README.md && git commit -am "docs: ajuste para a demo" && git push -u origin demo/video-fase4
gh pr create --title "Demo: deploy automatizado" --body "PR da demonstração em vídeo"
```
Espere o check **"Build, testes (cobertura >= 80%) e SonarCloud"** do PR ficar verde (~4 min) **antes** de gravar.

**0.4. Interface do RabbitMQ.** Num terminal separado, deixe o túnel aberto durante toda a gravação:
```bash
ssh -i "/d/ProjetosVSS/Pós/oficina-mecanica-infra-k8s/k3s_ec2_key" -L 15672:localhost:15672 ubuntu@32.193.30.44 \
  "sudo k3s kubectl port-forward -n oficina-mecanica svc/rabbitmq 15672:15672"
```
Acesse `http://localhost:15672` com usuário `oficina` e a senha do secret `RABBITMQ_PASSWORD`. Se não lembrar a senha:
```bash
ssh -i "/d/ProjetosVSS/Pós/oficina-mecanica-infra-k8s/k3s_ec2_key" ubuntu@32.193.30.44 \
  "sudo k3s kubectl get secret rabbitmq-credentials -n oficina-mecanica -o jsonpath='{.data.RABBITMQ_DEFAULT_PASS}' | base64 -d"
```

**0.5. Abas abertas no navegador**

| Aba | URL |
|---|---|
| README do OS (diagramas) | `github.com/konrath9/OficinaMecanica#fase-4--microsserviços-e-saga-pattern` |
| 3 repos | `github.com/konrath9/OficinaMecanica` · `.../oficina-billing-service` · `.../oficina-execution-service` |
| PR da demo | o PR do passo 0.3 |
| Actions do Billing | `github.com/konrath9/oficina-billing-service/actions` |
| SonarCloud | `sonarcloud.io/organizations/konrath9/projects` |
| RabbitMQ | `http://localhost:15672` → aba **Queues** |
| New Relic — mapa de serviços | one.newrelic.com → **APM & Services** → `oficina-mecanica-api` → **Service map** |
| New Relic — traces | one.newrelic.com → **APM & Services** → `oficina-mecanica-api` → **Distributed tracing** |
| New Relic — dashboard | o dashboard da Fase 3 |
| Código da saga | `github.com/konrath9/OficinaMecanica/blob/master/OficinaMecanica.Infrastructure/Mensageria/OrdemServicoStateMachine.cs` |

**0.6. Dois terminais**, com fonte grande:
- **T1**: na pasta do OS, com `H=http://32.193.30.44` exportado.
- **T2**: logs do Billing no cluster:
  ```bash
  ssh -i "/d/ProjetosVSS/Pós/oficina-mecanica-infra-k8s/k3s_ec2_key" ubuntu@32.193.30.44
  sudo k3s kubectl logs -f deployment/oficina-billing-service -n oficina-mecanica
  ```

---

## Parte 1 — Abertura e arquitetura · 0:00 → 1:45

**Mostrar:** o README do OS, rolando até o diagrama da arquitetura.

**Falar:**
> "Fase 4 do Tech Challenge. A oficina passou a operar com várias filiais, e o monolito das fases anteriores foi dividido
> em três microsserviços independentes, cada um com repositório, banco de dados, pipeline e deploy no Kubernetes próprios.
>
> O **OS Service** cuida da ordem de serviço: abertura, status e histórico, além de clientes e veículos. Ele também é o
> **orquestrador da Saga**. O **Billing Service** gera o orçamento e cuida do pagamento, integrado ao **Mercado Pago**. O
> **Execution Service** gerencia a fila de execução e o reparo.
>
> Os dois primeiros usam **PostgreSQL**. O Execution usa **MongoDB**, porque a execução é um documento autocontido, com os
> serviços e o histórico aninhados, sem joins.
>
> Nenhum serviço acessa o banco de outro. O cliente fala com eles por **REST**, através do Traefik. Entre eles, a
> comunicação é **assíncrona, por mensageria, via RabbitMQ**."

---

## Parte 2 — Repositórios e proteção · 1:45 → 2:45

**Mostrar:** os 3 repositórios e, em um deles, **Settings → Branches**.

**Falar:**
> "Três repositórios, cada um com seu Dockerfile, manifestos Kubernetes e pipeline. A branch principal é protegida em
> todos: só entra código por Pull Request, e o check de build e testes precisa passar antes do merge."

---

## Parte 3 — Deploy automatizado com validação de testes · 2:45 → 5:30

**Mostrar:** o PR da demo (passo 0.3), com o check verde.

**Falar:**
> "Este PR no Billing Service já passou pelo check obrigatório. Vou mergear e o deploy acontece sozinho."

Clique em **Merge** e vá para **Actions** → o run que começou. Abra o `ci-cd.yml` do repositório.

**Falar:**
> "A pipeline tem três etapas. Na primeira, faz build, roda os **testes unitários e de integração com gate de cobertura
> de 80%**, ou seja, abaixo disso o pipeline falha, e manda a análise para o **SonarCloud**. Na segunda, gera a imagem,
> publica no GitHub Container Registry e sobe um **cluster Kubernetes efêmero** dentro do próprio runner, com RabbitMQ e
> banco, aplica os manifestos e faz um smoke test. Só se tudo isso passar vem a terceira, o **deploy no cluster k3s de
> produção** na AWS."

**Mostrar** o run do passo 0.1, já concluído, abrindo o job de testes e rolando até a tabela de cobertura do coverlet:
> "Aqui a validação dos testes: 48 testes, com cobertura bem acima dos 80%."

**Mostrar** o SonarCloud com os três projetos:
> "E a análise de qualidade de cada serviço: cobertura, bugs, vulnerabilidades."

Volte ao run do merge. Se já tiver passado da etapa de testes, mostre-a verde. Não espere o fim; volte a ele na Parte 6.

---

## Parte 4 — Fluxo completo de uma OS pelos microsserviços · 5:30 → 8:30

**Mostrar:** a aba **Queues** do RabbitMQ:
> "Estas são as filas que os serviços criaram no RabbitMQ: os comandos que a Saga manda, como gerar-orcamento,
> aprovar-orcamento, estornar-pagamento e iniciar-execucao, e a fila da própria Saga, ordem-servico-saga-state, onde
> chegam as respostas do Billing e do Execution."

**T1**, com **pagamento real no sandbox do Mercado Pago**:
```bash
OS_URL=$H BILLING_URL=$H EXECUTION_URL=$H node scripts/demo-saga.mjs checkout
```
Quando o script imprimir o link de pagamento, abra-o no navegador, entre com o **comprador de teste** e pague com um
cartão de teste, com o nome do titular **APRO**. O Mercado Pago chama o webhook do Billing e o script continua sozinho.
Se o webhook não chegar em 1 a 2 minutos (o ambiente é `http`), use o plano B: rode o cenário `feliz`, que confirma o
pagamento pelo `/simular`, e diga na narração que é a confirmação simulada.

Narre conforme as linhas aparecem. O script imprime o status da OS **e** o estado da Saga a cada passo:
> "Abrimos uma OS no **OS Service** com um serviço e uma peça.
>
> Quando o técnico conclui o diagnóstico, a Saga publica **GerarOrcamento**. Repare que a OS continua em diagnóstico e a
> saga está em **AguardandoOrcamento**: o status só muda quando o **Billing** confirma que gerou o orçamento.
>
> O Billing responde **OrcamentoGerado** e a OS vai para aguardando aprovação. Aqui está o orçamento consultado direto no
> Billing Service.
>
> O cliente aprova. A Saga pede a cobrança ao Billing, que gera o **link de pagamento do Mercado Pago**.
>
> Pagamento confirmado: a Saga publica **IniciarExecucao** e a OS entra na fila do **Execution Service**, gravada no MongoDB.
>
> O mecânico faz o diagnóstico e o reparo pelo Execution. Quando o último serviço termina, ele publica
> **ExecucaoFinalizada**, a Saga finaliza a OS, e a recepção registra a entrega."

**Mostrar**, no T2, os logs do Billing que apareceram durante o fluxo:
> "E aqui o Billing recebendo os comandos pelo RabbitMQ."

---

## Parte 5 — Saga Pattern e tratamento de falhas · 8:30 → 11:30

**Mostrar:** o `OrdemServicoStateMachine.cs` no GitHub, rolando pelos blocos `During(...)`.

**Falar:**
> "Escolhemos a **Saga orquestrada**. O fluxo da OS é sequencial e tem um dono natural, o OS Service. Com orquestração,
> toda a sequência e todas as compensações ficam neste arquivo, numa state machine do MassTransit. Billing e Execution só
> recebem comandos e respondem, sem conhecer o fluxo.
>
> O estado da saga é gravado no Postgres na **mesma transação** da mudança da OS e das mensagens a publicar. É o padrão
> outbox: se o serviço cair no meio, nada se perde e nada é publicado em dobro."

**T1, falha 1: pagamento recusado**
```bash
OS_URL=$H BILLING_URL=$H EXECUTION_URL=$H node scripts/demo-saga.mjs pagamento-recusado
```
> "Agora o Mercado Pago recusa o pagamento. A Saga compensa: manda o Billing **cancelar o orçamento** e cancela a OS. Os
> dois serviços terminam consistentes: OS cancelada e orçamento cancelado."

**T1, falha 2: falha durante a execução**
```bash
OS_URL=$H BILLING_URL=$H EXECUTION_URL=$H node scripts/demo-saga.mjs falha-execucao
```
> "O caso mais interessante: a OS já foi paga e está em reparo quando o mecânico descobre que a peça está indisponível.
> O Execution reporta a falha. Repare na saga passando por **Estornando**: ela pede ao Billing o **estorno do pagamento**
> e, só quando o estorno é confirmado, cancela a OS. Ninguém fica pagando por um serviço que não foi feito."

---

## Parte 6 — Monitoramento e rastreamento distribuído · 11:30 → 14:00

**Mostrar:** New Relic → **APM & Services**:
> "Os três serviços enviam traces, métricas e logs para o New Relic por OpenTelemetry, a mesma ferramenta da Fase 3."

**Service map** do `oficina-mecanica-api`:
> "O mapa de serviços mostra quem fala com quem: o OS Service, o RabbitMQ, o Billing e o Execution."

**Distributed tracing** → abra um trace do `POST /api/ordens-servico/{id}/concluir-diagnostico`:
> "E aqui o rastreamento distribuído de uma única operação. O técnico chamou o OS Service, que publicou no RabbitMQ, e o
> mesmo trace continua no Billing, consumindo a mensagem e gravando o orçamento. O contexto do trace viaja dentro da
> mensagem, então eu acompanho uma OS atravessando os três serviços, com a duração de cada etapa."

**Dashboard**:
> "No dashboard, o volume de OS e o tempo médio por status, alimentados pelas execuções que acabamos de fazer."

Volte ao **Actions** do Billing (Parte 3):
> "E o deploy que disparamos no começo terminou: build, testes, cluster efêmero e produção, tudo verde."

---

## Parte 7 — Fechamento · 14:00 → 14:30

> "Recapitulando: três microsserviços independentes com banco relacional e NoSQL, comunicação síncrona por REST e
> assíncrona pelo RabbitMQ, Saga orquestrada com compensação em cada etapa, testes unitários e BDD com mais de 80% de
> cobertura em todos os serviços, qualidade no SonarCloud, deploy automatizado em Kubernetes e rastreamento distribuído no
> New Relic. Os links estão no PDF da entrega. Obrigado!"

---

## Colinha

| | |
|---|---|
| Produção (Traefik) | `http://32.193.30.44` · Swagger: `/swagger`, `/billing/swagger`, `/execucao/swagger` |
| Login por CPF (Lambda) | `https://vmdccz407g.execute-api.us-east-1.amazonaws.com/auth/login` |
| Demo da saga | `node scripts/demo-saga.mjs [feliz \| pagamento-recusado \| falha-execucao \| checkout \| checkout-falha]` (`checkout*` = pagamento real no Mercado Pago) |
| Estado da saga de uma OS | `GET /api/ordens-servico/{id}/saga` |
| RabbitMQ UI | `http://localhost:15672` (via túnel do passo 0.4), usuário `oficina` |

## Dicas de gravação

- Grave em blocos (uma parte por vez) e junte depois.
- Se o New Relic não mostrar o trace atravessando os serviços, rode mais algumas demos e espere 2 a 3 minutos antes da Parte 6.
- Confirme a duração **abaixo de 15:00** antes de subir no YouTube (público ou não listado).
