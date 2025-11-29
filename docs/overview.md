# Crepe Controlador – Visão Técnica

Este documento resume como o projeto está organizado (Angular + .NET 6), como as telas conversam com a API e os principais pontos de regras e componentes para que você consiga navegar e estender o código sem depender de consultas rápidas ao assistente.

## Estrutura geral

- `CrepeControladorFrontend/`: SPA Angular que entrega dashboards, gestão de pedidos e catálogo de itens.
- `CrepeControladorApi/`: API ASP.NET Core 6 hospedada em container Linux; usa PostgreSQL via EF Core e procedures já existentes.
- `.dockerignore`, `Dockerfile` e `src/_redirects` estão configurados para o deploy (API em container; SPA no Netlify com fallback para `index.html`).

Fluxo de dados típico:

1. O usuário interage com uma página Angular (por exemplo “Pedidos abertos”).
2. A página usa um serviço Angular (`PedidoService` ou `DadosService`) para montar a URL baseando-se no `environment.apiUrl`.
3. O serviço chama a API REST (`/api/pedidos`, `/api/dashboard/...` etc.) via `HttpClient`.
4. A API usa EF Core para invocar stored procedures (prefetch/CRUD) ou o próprio contexto para persistir pedidos/itens.
5. Respostas voltam para a SPA, que atualiza os estados locais / Subjects e re-renderiza as telas e modais.

## Frontend (Angular)

### Tecnologias e configuração

- Angular standalone components (sem módulos) + FormsModule/template-driven forms.
- `app.routes.ts` registra as rotas: `/dados`, `/pedidos/abertos`, `/pedidos/fechados`, `/itens`.
- `environment.ts` usa `/api/` local e `environment.prod.ts` aponta para a URL do backend publicado.
- `src/_redirects` garante SPA redirects no Netlify (`/* -> /index.html 200`).
- `PedidoService` e `DadosService` encapsulam chamadas HTTP, reutilizando um `Subject` (`atualizacoes$`) para avisar telas de pedidos sempre que algo muda.

### Páginas principais

#### Pedidos abertos (`src/app/pages/pedidos-abertos`)

- Mantém dois painéis: resumo por status (“Preparando”/“Pronto”) e agrupamento por tipo (“Entrega”/“Restaurante”).
- Modais: o overlay é controlado pela flag booleana `modalDetalhesAberto`. Quando um pedido é selecionado:
  - Busca detalhes em `PedidoService.obterPedido`.
  - Preenche `pedidoFormulario`.
  - Mostra o modal (`div.modal-overlay-detalhe`) que captura o clique para fechar.
- Formulário no modal:
  - `pedidoFormulario.itens` é um array mutável; cada item tem `itemId` e `quantidade`.
  - Botões “Salvar” e “Finalizar/Cancelar pedido” chamam `PedidoService.atualizarPedido` com payload montado em `montarPayloadAtualizacao`.
  - Validações básicas garantem que haja pelo menos um item e quantidades > 0.
- Há também a ação rápida `finalizarPedidoDireto` que busca o detalhe, força status “Finalizado” e notifica os demais componentes via `atualizacoes$`.

#### Pedidos fechados (`src/app/pages/pedidos-fechados`)

- Lista pedidos com `PedidoService.listarPorGrupoStatus('FECHADOS')`.
- Filtra localmente entre finalizados/cancelados e faz paginação (10 por página).
- Modal simples de leitura (`modalDetalhesAberto`) mostra o detalhamento carregado sob demanda.

#### Itens (`src/app/pages/itens`)

- Usa `PedidoService.listarItens` para carregar o catálogo.
- Modal `modalItemAberto` reaproveita o mesmo formulário para criar/editar itens e é alimentado por `novoItem`.
- Alternar ativo/inativo chama `PedidoService.atualizarItem` e atualiza as listas locais (`recalcularGrupos`).

#### Dados / Dashboards (`src/app/pages/dados`)

- Filtro de período (Dia, Semana, Mês, Total, Personalizado) controla as datas enviadas aos endpoints.
- Chama múltiplos observables em paralelo (`forkJoin`):
  - `dashboard/resumo`
  - `dashboard/tipo-pedido`
  - `dashboard/itens-ranking`
  - `dashboard/horarios/periodo`
  - Caso haja pelo menos 7 dias, habilita análises semanais e chama `dashboard/dia-semana/picos` e `.../distribuicao`.
- Constrói 3 visualizações principais:
  - **Pedidos e faturamento por hora**: gráfico de barras com dados de `DashboardHorarioPico`.
  - **Horários com maior movimento (por dia da semana)**: mostra apenas o pico diário, vindo de `picosDiaSemana`.
  - **Pedidos por hora e dia da semana**: heatmap onde cada coluna é um dia e cada linha uma hora, calculando `percentual` relativo ao maior volume global.

### Padrões de UI reutilizados

- **Modais**: overlay + card central com `*ngIf`. O clique no overlay chama `fecharModal...`, mas `click` dentro do card usa `stopPropagation` para evitar fechar.
- **Loaders**: flags `carregando`, `carregandoDetalhes`, `salvandoItem` etc. habilitam `<div class="estado carregando">` ou `<div class="pedidos-loading-overlay">`.
- **Mensagens**: campos como `mensagemFormulario` informam validações específicas na tela.
- **Atualizações cross-component**: `PedidoService.notificarAtualizacao()` emite no `Subject` para que tanto pedidos abertos quanto fechados recarreguem listas.

## Backend (.NET 6 + PostgreSQL)

### Inicialização (`Program.cs`)

- Registra controllers, Swagger (ambiente dev) e CORS liberando `http://localhost:4200`, `https://localhost:4200` e `https://crepefael.netlify.app`.
- Injeta `AppDbContext` (Npgsql) e os serviços `DashboardService` e `PedidoQueryService`.
- `appsettings.json` guarda `ConnectionStrings:DefaultConnection`.

### Camada de acesso a dados

- `AppDbContext` define `DbSet<Pedido>`, `DbSet<Item>` e `DbSet<ItensPedido>` e configura constraints (tamanhos, relacionamentos).
- Regras complexas moram nos stored procedures (prefixo `sp_...`). O EF Core consome via `FromSqlRaw` ou comandos diretos (`DbCommand`) para aproveitar resultados tabulares personalizados.

### Serviços

- **`DashboardService`**: encapsula chamadas para procedures `sp_Dashboard_*` (resumo, ranking, horários). Ele recebe `DateTime` de entrada, injeta parâmetros e mapeia Dtos (`DashboardResumoPeriodoDto`, `DashboardHorarioPicoDto` etc.). Também calcula o período total diretamente no DbContext (filtrando pedidos com status “Finalizado”).
- **`PedidoQueryService`**: expõe métodos de leitura específicos (`PesquisarPedidosAsync`, `ListarPedidosAbertosPorTipoAsync`, `ListarPorGrupoStatusAsync`). Cada método abre manualmente um `DbConnection`, executa a procedure desejada e usa `MapearPedidosAsync` para converter em `PedidoResumoDto`.

### Controllers e rotas

| Controller/rota                       | Verbo | Responsabilidade                                                                                            |
|--------------------------------------|-------|-------------------------------------------------------------------------------------------------------------|
| `GET /api/dashboard/resumo`          | GET   | KPIs do período (quantidade de pedidos, ticket médio, faturamento total etc.).                              |
| `GET /api/dashboard/horarios/periodo`| GET   | Histogramas hora × pedidos/faturamento dentro do intervalo filtrado.                                       |
| `GET /api/dashboard/dia-semana/picos`| GET   | Apenas o horário de pico de cada dia da semana (usado no cards “Horários com maior movimento”).            |
| `GET /api/dashboard/dia-semana/distribuicao` | GET | Matriz de todas as horas por dia da semana (heatmap).                                                       |
| `GET /api/dashboard/tipo-pedido`     | GET   | Distribuição percentual por tipo (Restaurante, Entrega, etc.).                                              |
| `GET /api/dashboard/itens-ranking`   | GET   | Ranking dos itens mais vendidos.                                                                            |
| `GET /api/dashboard/periodo-total`   | GET   | Data mínima/máxima considerando pedidos finalizados (libera filtros “Total”).                              |
| `GET /api/pedidos`                   | GET   | Lista completa de pedidos (`sp_Pedido_Obter`).                                                              |
| `GET /api/pedidos/{id}`              | GET   | Pedido + itens (consulta principal + procedure `sp_ItensPedido_ObterPorPedido`).                            |
| `GET /api/pedidos/pesquisar?termo=`  | GET   | Busca textual (usa `PedidoQueryService`).                                                                  |
| `GET /api/pedidos/abertos?tipoPedido=` | GET | Lista aberta por tipo.                                                                                      |
| `GET /api/pedidos/grupo-status?grupo=ABERTOS|FECHADOS` | Agrupa por status.                                                                              |
| `POST /api/pedidos`                  | POST  | Cria pedido. Valida itens, gera código `Pedido #XXXX`, calcula totais e salva `Pedido` + `ItensPedido`.     |
| `PUT /api/pedidos/{id}`              | PUT   | Atualiza pedido aberto (cliente, status, itens). Se status mudar para finalizado/cancelado grava `DataConclusao`. |
| `GET /api/pedidos/buscar?termo=`     | GET   | Endpoint herdado/alternativo para busca (via `sp_PesquisarPedidos`).                                       |
| `GET /api/itens` / `GET /api/itens/{id}` | GET | CRUD do catálogo lendo `sp_Item_Obter`.                                                                    |
| `POST /api/itens` / `PUT /api/itens/{id}` | POST/PUT | Criação e edição de itens (nome, preço, ativo).                                                       |

### Fluxo da API de criação/edição de pedidos

1. **Validação**: `PedidosController.CriarPedido` garante que o DTO tenha itens, suas quantidades e itens válidos.
2. **Geração de código**: `GerarCodigoPedidoAsync` pega o último ID e gera `Pedido #000X`.
3. **Construção da entidade**: popula `Pedido` com DataCriacao (UTC) + itens convertidos para `ItensPedido`.
4. **Cálculo de totais**: soma `Quantidade * Preco` de cada item e grava em `Pedido.ValorTotal`.
5. **Persistência**: usa `_context.Pedidos.Add()` e `SaveChangesAsync()`; EF configura relacionamentos.
6. **Retorno**: devolve `201 Created` com payload básico (`Id`, `Codigo`, `Status`, itens e totais).
7. **Atualização (`PUT`)**: carrega o pedido existente, valida se o status atual permite alteração (`Preparando`/`Pronto`), substitui itens, atualiza valor total e seta `DataConclusao` se o novo status for “Finalizado” ou “Cancelado”.

### Como a API acessa o banco

- Leituras complexas usam stored procedures já prontas; o código apenas passa parâmetros (`@DataInicio`, `@TipoPedido` etc.).
- Quando precisa materializar objetos ricos (ex.: itens do pedido), abre manualmente um `DbDataReader` e faz o mapping campo a campo.
- Atualizações/inserts são feitos via EF Core padrão para aproveitar change tracking.

## Deploy e observações operacionais

- **Frontend**: `ng build` gera `dist/CrepeControladorFrontend/browser`. O arquivo `_redirects` é copiado para o output (configurado no `angular.json`) para que o Netlify faça fallback para `index.html` em qualquer rota (`/pedidos/abertos`, `/dados` etc.).
- **Backend**: Dockerfile multipstage copia o código uma vez, roda `dotnet restore` e `dotnet publish -c Release -o /app/out`. A imagem final usa `mcr.microsoft.com/dotnet/aspnet:6.0` e executa `dotnet CrepeControladorApi.dll`. `.dockerignore` impede que `bin/`/`obj/` gerados em Windows entrem no build.
- **CORS/URL base**: mantenha `environment.prod.ts.apiUrl` apontando para o host público da API (já incluindo `/api`). O builder de URL nos serviços normaliza tanto `/api/` locais quanto URLs absolutas.

## Próximos passos possíveis

- Expandir este documento com diagramas/prints dos fluxos (por exemplo, sequência de criação de pedidos).
- Adicionar comentários TSDoc/XML em pontos de regra de negócio mais sensíveis (ex.: `PedidosController` e `dados.ts`).
- Publicar um guia rápido de “Como rodar localmente” com comandos `npm install`, `npm start`, `dotnet run`, variáveis de ambiente e dependências de banco.

Com este panorama você consegue localizar rapidamente onde cada funcionalidade mora (tanto no front quanto no back) e como as telas conversam com a API/stored procedures. Qualquer ajuste extra pode ser incorporado a esta doc adicionando subseções específicas (ex.: “Workflow do modal de itens”). 
