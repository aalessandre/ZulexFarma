USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto]    Script Date: 25/04/2026 18:36:10 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto](
	[CodigoProduto] [int] NOT NULL,
	[CodigoBarra] [varchar](15) NULL,
	[NomeProduto] [varchar](100) NOT NULL,
	[CodigoFormula] [int] NULL,
	[CodigoFabricante] [int] NULL,
	[CodigoGrupo] [smallint] NOT NULL,
	[DataCadastro] [smalldatetime] NOT NULL,
	[RegistroMS] [varchar](13) NULL,
	[StatusPreco] [tinyint] NOT NULL,
	[UnidadeEmbalagem] [varchar](5) NULL,
	[QuantidadePorEmbalagem] [smallint] NULL,
	[CodigoClasseTerapeutica] [smallint] NULL,
	[TipoLista] [tinyint] NULL,
	[CodigoAntigo] [varchar](15) NULL,
	[UnidadeCompra] [varchar](5) NULL,
	[UnidadeVenda] [varchar](5) NULL,
	[FracaoVenda] [smallint] NOT NULL,
	[PrecoFP] [money] NOT NULL,
	[NCM] [varchar](10) NULL,
	[CodigoGrupoInventario] [tinyint] NULL,
	[CodigoSessao] [int] NULL,
	[Imagem] [varbinary](max) NULL,
	[Eliminado] [bit] NOT NULL,
	[IPI] [smallmoney] NOT NULL,
	[ReducaoIPI] [smallmoney] NOT NULL,
	[II] [smallmoney] NOT NULL,
	[PisCofinsCST] [varchar](2) NULL,
	[PisCofinsNatureza] [varchar](3) NULL,
	[PisCofinsCSTEntrada] [varchar](2) NULL,
	[CEST] [varchar](7) NULL,
	[Origem] [tinyint] NULL,
	[IsentoAnvisa] [bit] NOT NULL,
	[StatusPrecoPMC] [tinyint] NULL,
	[CodigoBeneficio] [varchar](8) NULL,
	[PrecoBolsaFamilia] [money] NOT NULL,
 CONSTRAINT [PK_Produto] PRIMARY KEY CLUSTERED 
(
	[CodigoProduto] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_DataCadastro]  DEFAULT (getdate()) FOR [DataCadastro]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_StatusPreco]  DEFAULT ((0)) FOR [StatusPreco]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produtos_FracaoVenda]  DEFAULT ((1)) FOR [FracaoVenda]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_PrecoFP]  DEFAULT ((0)) FOR [PrecoFP]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_Eliminado]  DEFAULT ((0)) FOR [Eliminado]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_IPI]  DEFAULT ((0)) FOR [IPI]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_ReducaoIPI]  DEFAULT ((0)) FOR [ReducaoIPI]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_II]  DEFAULT ((0)) FOR [II]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_IsentoAnvisa]  DEFAULT ((0)) FOR [IsentoAnvisa]
GO

ALTER TABLE [dbo].[Produto] ADD  CONSTRAINT [DF_Produto_PrecoBolsaFamilia]  DEFAULT ((0)) FOR [PrecoBolsaFamilia]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Fiscal_PISCofins] FOREIGN KEY([PisCofinsCST])
REFERENCES [dbo].[Fiscal_PISCofins] ([CodigoCST])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Fiscal_PISCofins]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Fiscal_PISCofins_Entrada] FOREIGN KEY([PisCofinsCSTEntrada])
REFERENCES [dbo].[Fiscal_PISCofins] ([CodigoCST])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Fiscal_PISCofins_Entrada]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_ClasseTerapeutica] FOREIGN KEY([CodigoClasseTerapeutica])
REFERENCES [dbo].[Produto_ClasseTerapeutica] ([CodigoClasseTerapeutica])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_ClasseTerapeutica]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Fabricante] FOREIGN KEY([CodigoFabricante])
REFERENCES [dbo].[Produto_Fabricante] ([CodigoFabricante])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Fabricante]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Formula] FOREIGN KEY([CodigoFormula])
REFERENCES [dbo].[Produto_Formula] ([CodigoFormula])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Formula]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Grupo] FOREIGN KEY([CodigoGrupo])
REFERENCES [dbo].[Produto_Grupo] ([CodigoGrupo])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Grupo]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Inventario_Grupo] FOREIGN KEY([CodigoGrupoInventario])
REFERENCES [dbo].[Produto_Inventario_Grupo] ([CodigoGrupoInventario])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Inventario_Grupo]
GO

ALTER TABLE [dbo].[Produto]  WITH CHECK ADD  CONSTRAINT [FK_Produto_Produto_Sessao] FOREIGN KEY([CodigoSessao])
REFERENCES [dbo].[Produto_Sessao] ([CodigoSessao])
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Sessao]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Unidade_Compra] FOREIGN KEY([UnidadeCompra])
REFERENCES [dbo].[Produto_Unidade] ([Sigla])
ON UPDATE CASCADE
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Unidade_Compra]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Unidade_Embalagem] FOREIGN KEY([UnidadeEmbalagem])
REFERENCES [dbo].[Produto_Unidade] ([Sigla])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Unidade_Embalagem]
GO

ALTER TABLE [dbo].[Produto]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Produto_Unidade_Venda] FOREIGN KEY([UnidadeVenda])
REFERENCES [dbo].[Produto_Unidade] ([Sigla])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto] CHECK CONSTRAINT [FK_Produto_Produto_Unidade_Venda]
GO

