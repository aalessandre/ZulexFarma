USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_Fiscal_UF]    Script Date: 25/04/2026 18:37:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_Fiscal_UF](
	[UF] [char](2) NOT NULL,
	[CodigoProduto] [int] NOT NULL,
	[CodigoRegime] [tinyint] NOT NULL,
	[CodigoTributo] [tinyint] NOT NULL,
	[IcmsImportado] [smallmoney] NOT NULL,
	[ReducaoICMS] [smallmoney] NOT NULL,
	[IVA] [smallmoney] NOT NULL,
	[CodigoDecreto] [smallint] NULL,
	[ICMS] [smallmoney] NOT NULL,
	[CodigoFiscalID] [bigint] NULL,
	[FCP] [smallmoney] NOT NULL,
	[CodigoBeneficio] [varchar](8) NULL,
	[CodigoSegmentoTributario] [varchar](10) NULL,
 CONSTRAINT [PK_Produto_Fiscal_UF] PRIMARY KEY CLUSTERED 
(
	[UF] ASC,
	[CodigoProduto] ASC,
	[CodigoRegime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] ADD  CONSTRAINT [DF_Produto_Fiscal_UF_IcmsImportado]  DEFAULT ((0)) FOR [IcmsImportado]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] ADD  CONSTRAINT [DF_Produto_Fiscal_UF_ReducaoICMS]  DEFAULT ((0)) FOR [ReducaoICMS]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] ADD  CONSTRAINT [DF_Produto_Fiscal_UF_IVA]  DEFAULT ((0)) FOR [IVA]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] ADD  CONSTRAINT [DF_Produto_Fiscal_UF_ICMS]  DEFAULT ((0)) FOR [ICMS]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] ADD  CONSTRAINT [DF_Produto_Fiscal_UF_FCP]  DEFAULT ((0)) FOR [FCP]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Fiscal_UF_Produto] FOREIGN KEY([CodigoProduto])
REFERENCES [dbo].[Produto] ([CodigoProduto])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] CHECK CONSTRAINT [FK_Produto_Fiscal_UF_Produto]
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF]  WITH NOCHECK ADD  CONSTRAINT [FK_Produto_Fiscal_UF_Produto_NCM_Tributo] FOREIGN KEY([CodigoTributo])
REFERENCES [dbo].[Produto_NCM_Tributo] ([CodigoTributo])
NOT FOR REPLICATION 
GO

ALTER TABLE [dbo].[Produto_Fiscal_UF] CHECK CONSTRAINT [FK_Produto_Fiscal_UF_Produto_NCM_Tributo]
GO

