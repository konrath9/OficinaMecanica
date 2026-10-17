// Demonstracao ponta a ponta da Saga entre os 3 microsservicos (usada no video da Fase 4).
// Uso: node scripts/demo-saga.mjs [feliz|pagamento-recusado|falha-execucao]
// Por padrao aponta para o docker-compose local; em producao, passe as URLs do Traefik:
//   OS_URL=http://<K3S_HOST> BILLING_URL=http://<K3S_HOST> EXECUTION_URL=http://<K3S_HOST> node scripts/demo-saga.mjs
const OS = process.env.OS_URL ?? 'http://localhost:5000';
const BILLING = process.env.BILLING_URL ?? 'http://localhost:5001';
const EXECUTION = process.env.EXECUTION_URL ?? 'http://localhost:5002';
const cenario = process.argv[2] ?? 'feliz';

let token = '';
const sufixo = Date.now().toString().slice(-6);

async function http(metodo, url, body) {
  const r = await fetch(url, {
    method: metodo,
    headers: { 'Content-Type': 'application/json', ...(token && { Authorization: `Bearer ${token}` }) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const texto = await r.text();
  if (!r.ok) throw new Error(`${metodo} ${url} -> ${r.status} ${texto}`);
  return texto ? JSON.parse(texto) : null;
}

const passo = (msg) => console.log(`\n\x1b[36m▶ ${msg}\x1b[0m`);
const dormir = (ms) => new Promise((r) => setTimeout(r, ms));

async function aguardar(descricao, fn, tentativas = 40) {
  for (let i = 0; i < tentativas; i++) {
    const v = await fn().catch(() => null);
    if (v) return v;
    await dormir(500);
  }
  throw new Error(`Timeout aguardando: ${descricao}`);
}

async function statusOs(id) {
  const os = await http('GET', `${OS}/api/ordens-servico/${id}`);
  const saga = await http('GET', `${OS}/api/ordens-servico/${id}/saga`).catch(() => null);
  console.log(`  OS: ${os.status.padEnd(20)} Saga: ${saga?.currentState ?? '-'}`);
  return os.status;
}

const aguardarStatus = (id, status) =>
  aguardar(`OS em ${status}`, async () => (await statusOs(id)) === status);

// CPF valido gerado a partir de 9 digitos (para cada execucao criar um cliente novo)
function cpf() {
  const d = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  for (const n of [9, 10]) {
    const s = d.reduce((acc, v, i) => acc + v * (n + 1 - i), 0);
    d.push(((s * 10) % 11) % 10);
  }
  return d.join('');
}

passo('Login do staff (OS Service)');
const email = 'demo.saga@oficina.com', senha = 'Demo@123';
await http('POST', `${OS}/api/autenticacao/registrar`, { nome: 'Demo Saga', email, senha, perfil: 1 }).catch(() => {});
token = (await http('POST', `${OS}/api/autenticacao/login`, { email, senha })).token;

passo('Cadastro: cliente, veículo, serviço e peça');
const clienteId = (await http('POST', `${OS}/api/clientes`, { nome: 'Cliente Demo', documento: cpf(), email: 'cliente@demo.com' })).id;
const veiculoId = (await http('POST', `${OS}/api/veiculos`, { placa: `DEM${sufixo.slice(0, 1)}A${sufixo.slice(1, 3)}`, marca: 'VW', modelo: 'Gol', ano: 2018, clienteId })).id;
const servicoId = (await http('POST', `${OS}/api/servicos`, { nome: `Troca de pastilhas ${sufixo}`, descricao: 'Freio dianteiro', preco: 180 })).id;
const pecaId = (await http('POST', `${OS}/api/pecas`, { codigo: `PST-${sufixo}`, nome: 'Jogo de pastilhas', precoUnitario: 120, quantidadeEstoque: 10 })).id;

passo('Abertura da OS + itens (OS Service)');
const os = await http('POST', `${OS}/api/ordens-servico`, { clienteId, veiculoId, observacoes: `Demo Saga - cenário ${cenario}` });
console.log(`  OS ${os.numero} (${os.id})`);
await http('POST', `${OS}/api/ordens-servico/${os.id}/servicos`, { servicoId, quantidade: 1 });
await http('POST', `${OS}/api/ordens-servico/${os.id}/pecas`, { pecaId, quantidade: 1 });
await statusOs(os.id);

passo('Técnico conclui o diagnóstico -> Saga publica GerarOrcamento -> Billing Service');
await http('POST', `${OS}/api/ordens-servico/${os.id}/concluir-diagnostico`);
await aguardarStatus(os.id, 'AguardandoAprovacao');
const orcamento = await http('GET', `${BILLING}/api/orcamentos/${os.id}`);
console.log(`  Orçamento no Billing: R$ ${orcamento.valor} (${orcamento.status})`);

passo('Cliente aprova -> Saga publica AprovarOrcamento -> Billing cria a cobrança (Mercado Pago)');
await http('POST', `${OS}/api/acompanhamento/${os.numero}/aprovar`);
const pagamento = await aguardar('pagamento criado', () => http('GET', `${BILLING}/api/pagamentos/${os.id}`));
console.log(`  Pagamento: ${pagamento.status} - link: ${pagamento.linkPagamento}`);
await statusOs(os.id);

if (cenario === 'pagamento-recusado') {
  passo('Mercado Pago RECUSA o pagamento -> compensação: cancela orçamento e OS');
  await http('POST', `${BILLING}/api/pagamentos/${os.id}/simular`, { aprovado: false, motivo: 'cc_rejected_insufficient_amount' });
  await aguardarStatus(os.id, 'Cancelada');
  const orc = await aguardar('orçamento cancelado', async () => {
    const o = await http('GET', `${BILLING}/api/orcamentos/${os.id}`);
    return o.status === 'Cancelado' && o;
  });
  console.log(`  Orçamento no Billing: ${orc.status}`);
  process.exit(0);
}

passo('Mercado Pago aprova -> Saga publica IniciarExecucao -> Execution Service (MongoDB)');
await http('POST', `${BILLING}/api/pagamentos/${os.id}/simular`, { aprovado: true });
await aguardarStatus(os.id, 'EmExecucao');
const exec = await aguardar('OS na fila de execução', () => http('GET', `${EXECUTION}/api/execucoes/${os.id}`));
console.log(`  Execução: ${exec.status}`);

passo('Mecânico: diagnóstico e reparo (Execution Service)');
await http('POST', `${EXECUTION}/api/execucoes/${os.id}/iniciar-diagnostico`);
await http('POST', `${EXECUTION}/api/execucoes/${os.id}/iniciar-reparo`);
await http('PUT', `${EXECUTION}/api/execucoes/${os.id}/servicos/${servicoId}/iniciar`);

if (cenario === 'falha-execucao') {
  passo('Execução FALHA -> compensação: Billing estorna o pagamento -> OS cancelada');
  await http('POST', `${EXECUTION}/api/execucoes/${os.id}/falha`, { motivo: 'Disco de freio empenado - peça indisponível' });
  await aguardarStatus(os.id, 'Cancelada');
  const pg = await aguardar('pagamento estornado', async () => {
    const p = await http('GET', `${BILLING}/api/pagamentos/${os.id}`);
    return p.status === 'Estornado' && p;
  });
  console.log(`  Pagamento no Billing: ${pg.status}`);
  process.exit(0);
}

await http('PUT', `${EXECUTION}/api/execucoes/${os.id}/servicos/${servicoId}/finalizar`);
passo('Execution publica ExecucaoFinalizada -> OS finalizada');
await aguardarStatus(os.id, 'Finalizada');

passo('Entrega do veículo (OS Service)');
await http('POST', `${OS}/api/ordens-servico/${os.id}/registrar-entrega`);
await statusOs(os.id);
console.log('\n\x1b[32m✔ Fluxo completo concluído\x1b[0m');
