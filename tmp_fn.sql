CREATE OR REPLACE FUNCTION "fn_Pedidos_Abertos_PorMesa"(p_empresaid INT)
RETURNS TABLE(
    "MesaId" INT,
    "MesaNumero" VARCHAR,
    "Id" INT,
    "Codigo" VARCHAR,
    "Cliente" TEXT,
    "TipoPedido" VARCHAR,
    "Status" VARCHAR,
    "Observacao" TEXT,
    "ValorTotal" NUMERIC,
    "DataCriacao" TIMESTAMPTZ
) AS $$
    SELECT
        p."MesaId",
        COALESCE(m."Numero", 'Sem mesa') AS "MesaNumero",
        p."Id",
        p."Codigo",
        p."Cliente",
        p."TipoPedido",
        p."Status",
        p."Observacao",
        p."ValorTotal",
        p."DataCriacao"
    FROM "Pedidos" p
    LEFT JOIN "Mesas" m ON m."Id" = p."MesaId"
    WHERE p."EmpresaId" = p_empresaid
      AND p."TipoPedido" = 'Restaurante'
      AND p."Status" NOT IN ('Finalizado', 'Cancelado')
    ORDER BY COALESCE(m."Numero", 'Sem mesa'), p."DataCriacao" DESC;
$$ LANGUAGE sql STABLE;
