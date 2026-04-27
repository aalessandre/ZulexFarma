USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Produto_NCM]    Script Date: 25/04/2026 18:54:05 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Produto_NCM](
	[NCM] [varchar](10) NOT NULL,
	[Descricao] [varchar](max) NULL,
	[IPI] [smallmoney] NOT NULL,
	[ReducaoIPI] [smallmoney] NOT NULL,
	[II] [smallmoney] NOT NULL,
	[Pis] [smallmoney] NOT NULL,
	[Cofins] [smallmoney] NOT NULL,
	[PisCofinsCST] [varchar](2) NULL,
	[PisCofinsNatureza] [varchar](3) NULL,
	[PisCofinsCSTEntrada] [varchar](2) NULL,
	[PisCofinsNaturezaEntrada] [varchar](3) NULL,
	[Tributos] [smallmoney] NULL,
	[ICMSDiferenciado] [smallmoney] NULL,
	[CEST] [varchar](7) NULL,
 CONSTRAINT [PK_Produto_NCM] PRIMARY KEY CLUSTERED 
(
	[NCM] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Produto_NCM] ADD  CONSTRAINT [DF_Table1_ICMS2]  DEFAULT ((0)) FOR [IPI]
GO

ALTER TABLE [dbo].[Produto_NCM] ADD  CONSTRAINT [DF_Produto_NCM_ReducaoIPI]  DEFAULT ((0)) FOR [ReducaoIPI]
GO

ALTER TABLE [dbo].[Produto_NCM] ADD  CONSTRAINT [DF_Table1_ICMS1]  DEFAULT ((0)) FOR [II]
GO

ALTER TABLE [dbo].[Produto_NCM] ADD  CONSTRAINT [DF_Table1_ICMS3]  DEFAULT ((0)) FOR [Pis]
GO

ALTER TABLE [dbo].[Produto_NCM] ADD  CONSTRAINT [DF_Table1_ICMS1_1]  DEFAULT ((0)) FOR [Cofins]
GO

