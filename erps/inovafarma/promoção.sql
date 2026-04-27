USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Promocao]    Script Date: 25/04/2026 18:38:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Promocao](
	[CodigoPromocao] [int] NOT NULL,
	[NomePromocao] [varchar](100) NOT NULL,
	[DataInicio] [smalldatetime] NOT NULL,
	[DataFim] [smalldatetime] NULL,
	[PlanoVista] [bit] NOT NULL,
	[PlanoPrazo] [bit] NOT NULL,
	[PlanoCheque] [bit] NOT NULL,
	[PlanoCartao] [bit] NOT NULL,
	[Ativo] [bit] NOT NULL,
	[UltimoFiltro] [varchar](max) NULL,
	[MargemPrazo] [money] NULL,
	[AlteraPreco] [bit] NOT NULL,
	[ExclusivoConvenio] [bit] NOT NULL,
	[GerarComissao] [bit] NOT NULL,
	[Origem] [tinyint] NOT NULL,
	[Tipo] [tinyint] NOT NULL,
	[IDExterno] [varchar](10) NULL,
	[Versao] [varchar](10) NULL,
	[ExclusivoEcommerce] [bit] NOT NULL,
	[PlanoCarteiraDigital] [bit] NOT NULL,
	[LucroComBasePrecoPromocao] [bit] NOT NULL,
 CONSTRAINT [PK_Promocao] PRIMARY KEY CLUSTERED 
(
	[CodigoPromocao] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_Ativo]  DEFAULT ((1)) FOR [Ativo]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_AlteraPreco]  DEFAULT ((1)) FOR [AlteraPreco]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_ExclusivoConvenio]  DEFAULT ((0)) FOR [ExclusivoConvenio]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_GerarComissaoProdutos]  DEFAULT ((1)) FOR [GerarComissao]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_Origem]  DEFAULT ((0)) FOR [Origem]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_TipoDePromocao]  DEFAULT ((0)) FOR [Tipo]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF__Promocao__Exclus__310EEE34]  DEFAULT ((0)) FOR [ExclusivoEcommerce]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF_Promocao_PlanoCarteiraDigital]  DEFAULT ((0)) FOR [PlanoCarteiraDigital]
GO

ALTER TABLE [dbo].[Promocao] ADD  CONSTRAINT [DF__Promocao__LucroC__6582B673]  DEFAULT ((0)) FOR [LucroComBasePrecoPromocao]
GO

