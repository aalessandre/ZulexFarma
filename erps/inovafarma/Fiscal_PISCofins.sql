USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Fiscal_PISCofins]    Script Date: 26/04/2026 05:35:11 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Fiscal_PISCofins](
	[CodigoCST] [varchar](2) NOT NULL,
	[Descricao] [varchar](300) NOT NULL,
	[PISNaoCumulativo] [smallmoney] NOT NULL,
	[CofinsNaoCumulativo] [smallmoney] NOT NULL,
	[PISCumulativo] [smallmoney] NOT NULL,
	[CofinsCumulativo] [smallmoney] NOT NULL,
	[Tipo] [tinyint] NOT NULL,
	[OpcaoEntrada] [bit] NOT NULL,
	[OpcaoSaida] [bit] NOT NULL,
 CONSTRAINT [PK_Fiscal_PISCofins] PRIMARY KEY CLUSTERED 
(
	[CodigoCST] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Fiscal_PISCofins] ADD  CONSTRAINT [DF_Fiscal_PISCofins_PISNaoCumulativo]  DEFAULT ((0)) FOR [PISNaoCumulativo]
GO

ALTER TABLE [dbo].[Fiscal_PISCofins] ADD  CONSTRAINT [DF_Fiscal_PISCofins_CofinsNaoCumulativo]  DEFAULT ((0)) FOR [CofinsNaoCumulativo]
GO

ALTER TABLE [dbo].[Fiscal_PISCofins] ADD  CONSTRAINT [DF_Fiscal_PISCofins_PISNaoCumulativo1]  DEFAULT ((0)) FOR [PISCumulativo]
GO

ALTER TABLE [dbo].[Fiscal_PISCofins] ADD  CONSTRAINT [DF_Fiscal_PISCofins_CofinsNaoCumulativo1]  DEFAULT ((0)) FOR [CofinsCumulativo]
GO

ALTER TABLE [dbo].[Fiscal_PISCofins] ADD  CONSTRAINT [DF_Fiscal_PISCofins_OpcaoEntrada]  DEFAULT ((0)) FOR [OpcaoEntrada]
GO

ALTER TABLE [dbo].[Fiscal_PISCofins] ADD  CONSTRAINT [DF_Fiscal_PISCofins_OpcaoSaida]  DEFAULT ((0)) FOR [OpcaoSaida]
GO

