USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Fiscal_Natureza]    Script Date: 26/04/2026 05:34:31 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Fiscal_Natureza](
	[CodigoNatureza] [smallint] NOT NULL,
	[Natureza] [varchar](50) NOT NULL,
	[Operacao] [tinyint] NOT NULL,
	[CFOPTribPFDE] [varchar](5) NULL,
	[CFOPTribPFFE] [varchar](5) NULL,
	[CFOPTribPJFEComInsc] [varchar](5) NULL,
	[CFOPSubstituicaoDE] [varchar](5) NULL,
	[CFOPSubstituicaoFE] [varchar](5) NULL,
	[CFOPIsentoDE] [varchar](5) NULL,
	[CFOPIsentoFE] [varchar](5) NULL,
	[CFOPServicoDE] [varchar](5) NULL,
	[CFOPServicoFE] [varchar](5) NULL,
	[CalculaICMS] [bit] NULL,
	[CalculaIPI] [bit] NULL,
	[RelacionaNotaECF] [bit] NULL,
	[CFOPECFDE] [varchar](5) NULL,
	[CFOPECFFE] [varchar](5) NULL,
	[CFOPNTDE] [varchar](5) NULL,
	[CFOPNTFE] [varchar](5) NULL,
	[CodigoGrupoMovimentacao] [smallint] NULL,
	[CalculaST] [bit] NOT NULL,
	[PedeDI] [bit] NOT NULL,
	[CalculaPisCofins] [bit] NOT NULL,
	[Complementar] [bit] NOT NULL,
	[FinalidadeDevolucao] [bit] NOT NULL,
	[PrecoCusto] [bit] NOT NULL,
	[ObservacaoPadrao] [varchar](1500) NULL,
	[CodigoCST] [varchar](2) NULL,
	[CodigoTributo] [tinyint] NULL,
	[Finalidade] [tinyint] NOT NULL,
	[ReducaoICMS] [smallmoney] NOT NULL,
	[COP] [varchar](4) NULL,
	[CFOPOutrosDE] [varchar](5) NULL,
	[CFOPOutrosFE] [varchar](5) NULL,
	[CodigoCorrelacao] [int] NULL,
	[AtualizarCustoMedio] [bit] NOT NULL,
	[CFOPTribPJDEComInsc] [varchar](5) NULL,
 CONSTRAINT [PK_Fiscal_Natureza] PRIMARY KEY CLUSTERED 
(
	[CodigoNatureza] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF_Fiscal_Natureza_CalculaST]  DEFAULT ((0)) FOR [CalculaST]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF_Fiscal_Natureza_PedeDI]  DEFAULT ((0)) FOR [PedeDI]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF_Fiscal_Natureza_CalculaPisCofins]  DEFAULT ((0)) FOR [CalculaPisCofins]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF_Fiscal_Natureza_Complementar]  DEFAULT ((0)) FOR [Complementar]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF__Fiscal_Na__Final__7F81B441]  DEFAULT ((0)) FOR [FinalidadeDevolucao]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF__Fiscal_Na__Preco__184D620B]  DEFAULT ((0)) FOR [PrecoCusto]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF__Fiscal_Na__Final__202E7AF5]  DEFAULT ((1)) FOR [Finalidade]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF__Fiscal_Na__Reduc__21229F2E]  DEFAULT ((0)) FOR [ReducaoICMS]
GO

ALTER TABLE [dbo].[Fiscal_Natureza] ADD  CONSTRAINT [DF_Fiscal_Natureza_AtualizarCustoMedio]  DEFAULT ((1)) FOR [AtualizarCustoMedio]
GO

ALTER TABLE [dbo].[Fiscal_Natureza]  WITH NOCHECK ADD  CONSTRAINT [FK_Estoque_Movimentacao_Grupo_Fiscal_Natureza] FOREIGN KEY([CodigoGrupoMovimentacao])
REFERENCES [dbo].[Estoque_Movimentacao_Grupo] ([CodigoGrupo])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Natureza] CHECK CONSTRAINT [FK_Estoque_Movimentacao_Grupo_Fiscal_Natureza]
GO

ALTER TABLE [dbo].[Fiscal_Natureza]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Natureza_Fiscal_Correlacao_CFOPCST_Novo] FOREIGN KEY([CodigoCorrelacao])
REFERENCES [dbo].[Fiscal_Correlacao_CFOPCST_Novo] ([CodigoCorrelacao])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Natureza] CHECK CONSTRAINT [FK_Fiscal_Natureza_Fiscal_Correlacao_CFOPCST_Novo]
GO

ALTER TABLE [dbo].[Fiscal_Natureza]  WITH NOCHECK ADD  CONSTRAINT [FK_Fiscal_Natureza_Fiscal_PISCofins] FOREIGN KEY([CodigoCST])
REFERENCES [dbo].[Fiscal_PISCofins] ([CodigoCST])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Fiscal_Natureza] CHECK CONSTRAINT [FK_Fiscal_Natureza_Fiscal_PISCofins]
GO

