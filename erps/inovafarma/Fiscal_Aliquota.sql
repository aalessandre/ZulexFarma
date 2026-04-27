USE [INOVAFARMA_FARMACIAMAXIFARMA_FILIAL3]
GO

/****** Object:  Table [dbo].[Fiscal_Aliquota]    Script Date: 26/04/2026 05:35:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Fiscal_Aliquota](
	[CodigoAliquota] [int] NOT NULL,
	[UFOrigem] [char](2) NOT NULL,
	[UFDestino] [char](2) NOT NULL,
	[Aliquota] [real] NOT NULL,
	[Conveniado] [bit] NOT NULL,
	[GNRE] [bit] NOT NULL,
	[ReducaoRevendedor] [smallmoney] NOT NULL,
 CONSTRAINT [PK_Fiscal_Aliquota] PRIMARY KEY CLUSTERED 
(
	[CodigoAliquota] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Fiscal_Aliquota] ADD  CONSTRAINT [DF_Fiscal_Aliquota_UFOrigem]  DEFAULT ('SP') FOR [UFOrigem]
GO

ALTER TABLE [dbo].[Fiscal_Aliquota] ADD  CONSTRAINT [DF_Fiscal_Aliquota_Convenio]  DEFAULT ((0)) FOR [Conveniado]
GO

ALTER TABLE [dbo].[Fiscal_Aliquota] ADD  CONSTRAINT [DF_Fiscal_Aliquota_GNRE]  DEFAULT ((0)) FOR [GNRE]
GO

ALTER TABLE [dbo].[Fiscal_Aliquota] ADD  CONSTRAINT [DF__Fiscal_Al__Reduc__2AAC0968]  DEFAULT ((0)) FOR [ReducaoRevendedor]
GO

