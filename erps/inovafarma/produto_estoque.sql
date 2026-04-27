USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_Estoque]    Script Date: 25/04/2026 18:36:43 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_Estoque](
	[CodigoEstoque] [int] NOT NULL,
	[CodigoProduto] [int] NOT NULL,
	[CodigoEmpresa] [smallint] NOT NULL,
	[PrecoCompra] [money] NOT NULL,
	[PrecoCusto] [money] NOT NULL,
	[Lucro] [smallmoney] NOT NULL,
	[PrecoVenda] [money] NOT NULL,
	[PrecoPromocao] [money] NOT NULL,
	[UltimaCompra] [smalldatetime] NULL,
	[UltimaVenda] [smalldatetime] NULL,
	[Estoque] [numeric](10, 2) NOT NULL,
	[EstoqueDia] [numeric](10, 2) NOT NULL,
	[CodigoFornecedor] [int] NULL,
	[TemDesconto] [bit] NOT NULL,
	[DescontoMaximo] [smallmoney] NOT NULL,
	[NaoAtualiza] [bit] NOT NULL,
	[EstoqueMinimo] [int] NOT NULL,
	[EstoqueMaximo] [int] NOT NULL,
	[PrecoBaseST] [money] NOT NULL,
	[ValorST] [money] NOT NULL,
	[NaoRepassaComissao] [bit] NOT NULL,
	[CodigoStatus] [smallint] NULL,
	[Ativo] [bit] NOT NULL,
	[CodigoTributo] [tinyint] NULL,
	[Comissao] [money] NOT NULL,
	[CodigoGrupoPreco] [int] NULL,
	[CodigoPBMUltimaVenda] [tinyint] NULL,
	[Oferta] [bit] NOT NULL,
	[NaoAtualizaEstoqueMinimo] [bit] NOT NULL,
	[ValorIncentivo] [money] NOT NULL,
	[UsoContinuoVenda] [bit] NOT NULL,
	[CurvaABC] [char](1) NULL,
	[Mensagem] [varchar](500) NULL,
	[UltimaCompraValor] [money] NULL,
	[UltimaVendaValor] [money] NULL,
	[PenultimaCompra] [smalldatetime] NULL,
	[PenultimaCompraValor] [money] NULL,
	[EstoqueDemanda] [int] NOT NULL,
	[CodigoLocalizacao] [int] NULL,
	[PrecoVendaCaixa] [money] NOT NULL,
	[CompoePedidoCompra] [bit] NOT NULL,
	[PrecoCustoMedio] [money] NOT NULL,
	[DataPromocaoInicio] [smalldatetime] NULL,
	[DataPromocaoFim] [smalldatetime] NULL,
	[DataOfertaInicio] [smalldatetime] NULL,
	[DataOfertaFim] [smalldatetime] NULL,
	[CurvaABCAtualizada] [smalldatetime] NULL,
	[PMC] [money] NOT NULL,
	[EstoqueDemandaMinima] [int] NOT NULL,
	[CodigoTip] [int] NULL,
	[Outros] [money] NOT NULL,
	[DataUltimaAlteracao] [datetime] NULL,
	[AlterarPrecoRealEditaPrecoVenda] [bit] NOT NULL,
	[AlterarPrecoCustoEditaPrecoVenda] [bit] NOT NULL,
	[PermiteCoberturaDeOferta] [bit] NOT NULL,
 CONSTRAINT [PK_Produto_Estoque_1] PRIMARY KEY CLUSTERED 
(
	[CodigoEstoque] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoCompra]  DEFAULT ((0)) FOR [PrecoCompra]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoCusto]  DEFAULT ((0)) FOR [PrecoCusto]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_Lucro]  DEFAULT ((0)) FOR [Lucro]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoVenda]  DEFAULT ((0)) FOR [PrecoVenda]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoPromocao]  DEFAULT ((0)) FOR [PrecoPromocao]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_Estoque]  DEFAULT ((0)) FOR [Estoque]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_EstoqueDia]  DEFAULT ((0)) FOR [EstoqueDia]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_TemDesconto]  DEFAULT ((1)) FOR [TemDesconto]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_DescontoMaximo]  DEFAULT ((0)) FOR [DescontoMaximo]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_NaoAtualiza]  DEFAULT ((0)) FOR [NaoAtualiza]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_EstoqueMinimo]  DEFAULT ((0)) FOR [EstoqueMinimo]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_EstoqueMaximo]  DEFAULT ((0)) FOR [EstoqueMaximo]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoBaseST]  DEFAULT ((0)) FOR [PrecoBaseST]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_ValorST]  DEFAULT ((0)) FOR [ValorST]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_NaoRepassaComissao]  DEFAULT ((0)) FOR [NaoRepassaComissao]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_Ativo]  DEFAULT ((1)) FOR [Ativo]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_Comissao]  DEFAULT ((0)) FOR [Comissao]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_Oferta]  DEFAULT ((0)) FOR [Oferta]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_NaoAtualizaEstoqueMinimo]  DEFAULT ((0)) FOR [NaoAtualizaEstoqueMinimo]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_ValorIncentivo]  DEFAULT ((0)) FOR [ValorIncentivo]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_UsoContinuoVenda]  DEFAULT ((0)) FOR [UsoContinuoVenda]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_EstoqueDemanda1]  DEFAULT ((0)) FOR [EstoqueDemanda]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoVendaCaixa]  DEFAULT ((0)) FOR [PrecoVendaCaixa]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_CompoePedidoCompra]  DEFAULT ((1)) FOR [CompoePedidoCompra]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PrecoCustoMedio]  DEFAULT ((0)) FOR [PrecoCustoMedio]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PMC]  DEFAULT ((0)) FOR [PMC]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_EstoqueDemandaMinima]  DEFAULT ((0)) FOR [EstoqueDemandaMinima]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_Outros]  DEFAULT ((0)) FOR [Outros]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_AlterarPrecoRealEditaPrecoVenda]  DEFAULT ((0)) FOR [AlterarPrecoRealEditaPrecoVenda]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_AlterarPrecoCustoEditaPrecoVenda]  DEFAULT ((0)) FOR [AlterarPrecoCustoEditaPrecoVenda]
GO

ALTER TABLE [dbo].[Produto_Estoque] ADD  CONSTRAINT [DF_Produto_Estoque_PermiteCoberturaDeOferta]  DEFAULT ((1)) FOR [PermiteCoberturaDeOferta]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Compra_Tip] FOREIGN KEY([CodigoTip])
REFERENCES [dbo].[Compra_Tip] ([CodigoTip])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Compra_Tip]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Fornecedor] FOREIGN KEY([CodigoFornecedor])
REFERENCES [dbo].[Fornecedor] ([CodigoFornecedor])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Fornecedor]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Produto] FOREIGN KEY([CodigoProduto])
REFERENCES [dbo].[Produto] ([CodigoProduto])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Produto]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Produto_Grupo_Preco] FOREIGN KEY([CodigoGrupoPreco])
REFERENCES [dbo].[Produto_Grupo_Preco] ([CodigoGrupoPreco])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Produto_Grupo_Preco]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Produto_Localizacao] FOREIGN KEY([CodigoLocalizacao])
REFERENCES [dbo].[Produto_Localizacao] ([CodigoLocalizacao])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Produto_Localizacao]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Produto_NCM_Tributo] FOREIGN KEY([CodigoTributo])
REFERENCES [dbo].[Produto_NCM_Tributo] ([CodigoTributo])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Produto_NCM_Tributo]
GO

ALTER TABLE [dbo].[Produto_Estoque]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Estoque_Produto_Status] FOREIGN KEY([CodigoStatus])
REFERENCES [dbo].[Produto_Status] ([CodigoStatus])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Estoque] CHECK CONSTRAINT [FK_Produto_Estoque_Produto_Status]
GO

